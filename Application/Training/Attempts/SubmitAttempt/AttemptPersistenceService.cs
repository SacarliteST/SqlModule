using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Progress;
using SQLModule.Common.Results;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

/// <summary>Данные, которые нужно атомарно сохранить после оценки попытки.</summary>
/// <param name="Progress">Прохождение, к которому относится попытка.</param>
/// <param name="ModuleSession">Платформенная сессия или <see langword="null"/> для standalone.</param>
/// <param name="Version">Версия проверки (лимит попыток).</param>
/// <param name="Reservation">Резервация номера попытки, которую нужно завершить.</param>
/// <param name="Attempt">Новая попытка с результатом и snapshot.</param>
/// <param name="Checks">Оценённые критерии.</param>
/// <param name="Score">Итоговый балл попытки.</param>
/// <param name="FinishedAt">Момент завершения попытки.</param>
public sealed record AttemptPersistenceRequest(
    StudentTaskProgress Progress,
    ModuleSession? ModuleSession,
    TaskValidationVersion Version,
    AttemptReservation Reservation,
    Attempt Attempt,
    IReadOnlyList<EvaluatedCheck> Checks,
    int Score,
    DateTimeOffset FinishedAt);

/// <summary>Сохраняет результат попытки, финализацию прохождения и outbox-событие в одной транзакции.</summary>
public interface IAttemptPersistenceService
{
    /// <summary>Завершается ошибкой <see cref="ProgressErrors.Closed"/>, если прохождение закрылось за время проверки.</summary>
    Task<Result> SaveAsync(AttemptPersistenceRequest request, CancellationToken ct);

    /// <summary>Освобождает резервацию, не бросая исключений (для путей отказа).</summary>
    Task ReleaseReservationSafelyAsync(Guid reservationId);
}

public sealed class AttemptPersistenceService(
    IApplicationDbContext db,
    IAttemptReservationService reservationService,
    IProgressFinalizationService finalizationService,
    ILogger<AttemptPersistenceService> logger) : IAttemptPersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result> SaveAsync(AttemptPersistenceRequest request, CancellationToken ct)
    {
        var (progress, moduleSession, version, reservation, attempt, checks, score, finishedAt) = request;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await LockProgressAsync(progress.Id, ct);
            await db.Entry(progress).ReloadAsync(ct);
            if (progress.Status != ProgressStatus.Active)
            {
                // Пока эта попытка выполнялась в песочнице, конкурентная попытка уже заняла
                // последнее место и завершила прохождение (например, тоже набрала 100 баллов) —
                // ReserveAsync выше не мог это поймать заранее, статус проверяем только теперь,
                // под блокировкой строки прохождения.
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                await ReleaseReservationSafelyAsync(reservation.Id);
                return Result.Fail(ProgressErrors.Closed);
            }

            progress.RecordCountedAttempt(score);
            FinalizationReason? finalizationReason = null;
            if (score == 100)
            {
                finalizationReason = FinalizationReason.PerfectScore;
            }
            else if (version.MaxAttempts.HasValue && progress.AttemptsUsed >= version.MaxAttempts.Value)
            {
                finalizationReason = FinalizationReason.AttemptsExhausted;
            }

            if (finalizationReason.HasValue)
            {
                await finalizationService.PrepareAsync(
                    progress, moduleSession, finalizationReason.Value, finishedAt, attempt, ct);
            }

            db.Attempts.Add(attempt);
            db.AttemptCheckResults.AddRange(checks.Select(value => AttemptCheckResult.Create(
                attempt.Id, value.Check.Id, value.Check.Kind, value.Status,
                value.Check.Weight, value.AwardedScore, value.Check.Order, value.Message, null)));
            reservation.Complete(attempt.Id, finishedAt);
            if (moduleSession is not null)
            {
                AddPracticeEvent(moduleSession, attempt, finishedAt);
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            await ReleaseReservationSafelyAsync(reservation.Id);
            throw;
        }

        return Result.Success();
    }

    public async Task ReleaseReservationSafelyAsync(Guid reservationId)
    {
        try
        {
            db.ChangeTracker.Clear();
            await reservationService.ReleaseAsync(reservationId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось освободить резервацию попытки {ReservationId}",
                reservationId);
        }
    }

    private async Task LockProgressAsync(Guid progressId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = """SELECT "Id" FROM "StudentTaskProgresses" WHERE "Id" = @progressId FOR UPDATE""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "progressId";
        parameter.Value = progressId;
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private void AddPracticeEvent(ModuleSession session, Attempt attempt, DateTimeOffset occurredAt)
    {
        var eventId = Guid.NewGuid();
        var message = new PracticeEventMessage(
            session.Id, session.SessionKey, eventId, "sql_submit", occurredAt,
            new PracticeEventPayload(
                attempt.SubmittedSql, PracticeEventMapping.ToIntegrationStatus(attempt.Status),
                attempt.RowCount, attempt.DurationMs, attempt.IsCorrect, attempt.Reason.ToString()));
        db.PendingPublishes.Add(PendingPublish.Create(
            eventId, PendingPublishKind.Event, session.Id, $"event:{attempt.Id:D}",
            JsonSerializer.Serialize(message, JsonOptions), occurredAt));
    }
}
