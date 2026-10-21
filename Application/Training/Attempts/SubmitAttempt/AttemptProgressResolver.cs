using Microsoft.EntityFrameworkCore;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Progress;
using SQLModule.Common.Results;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

/// <summary>
/// Итог поиска прохождения: <see cref="Progress"/> пуст и ошибки нет — прохождения нет (запрос обслужит legacy-ветка).
/// </summary>
public sealed record ProgressResolution(StudentTaskProgress? Progress, Error? Error = null)
{
    public bool IsFailure => Error is not null;
}

/// <summary>Определяет прохождение для отправки решения: standalone или платформенная сессия.</summary>
public interface IAttemptProgressResolver
{
    Task<ProgressResolution> ResolveAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct);
}

public sealed class AttemptProgressResolver(
    IApplicationDbContext db,
    IPlatformProgressService platformProgressService,
    IPlatformStudentScope platformScope) : IAttemptProgressResolver
{
    public async Task<ProgressResolution> ResolveAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct)
    {
        var progress = moduleSession is null
            ? await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                .SingleOrDefaultAsync(value =>
                    value.UserId == command.UserId && value.TaskId == command.TaskId &&
                    value.ModuleSessionId == null && value.Status == ProgressStatus.Active, ct)
            : await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                .SingleOrDefaultAsync(value => value.ModuleSessionId == moduleSession.Id, ct);
        if (progress is null && moduleSession is not null)
        {
            // Проверяем закрытие/истечение сессии только когда для неё ещё нет прохождения:
            // если прохождение уже существует, эту же проверку по статусу прохождения делает
            // ReserveAsync — но она корректно пропускает идемпотентный повтор (тот же
            // Idempotency-Key), в отличие от проверки здесь, которая касается только
            // по-настоящему нового запроса на ещё не начатую платформенную сессию.
            var sessionState = await platformScope.ResolveAsync(
                command.UserId, moduleSession.Id, PlatformScopeMode.Write, ct);
            if (!sessionState.IsSuccess)
            {
                return new ProgressResolution(null, sessionState.Error!);
            }

            var created = await platformProgressService.EnsureCreatedAsync(moduleSession, ct);
            if (!created.IsSuccess)
            {
                return new ProgressResolution(null, created.Error!);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                progress = await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                    .SingleAsync(value => value.Id == created.Value!.Id, ct);
            }
            catch (DbUpdateException)
            {
                // Конкурентный запрос уже создал прохождение для этой же ModuleSessionId
                // (уникальный индекс IX_StudentTaskProgresses_ModuleSessionId) — подхватываем
                // его вместо падения в 500, дальше решает ReserveAsync. Отсоединяем только
                // неудавшуюся Added-запись, а не весь ChangeTracker.Clear() — иначе заодно
                // "теряется" отслеживание moduleSession, и его MarkCompletionPending() дальше
                // по коду молча не сохранится.
                var failedEntry = db.ChangeTracker.Entries<StudentTaskProgress>()
                    .FirstOrDefault(entry => entry.State == EntityState.Added);
                if (failedEntry is not null)
                {
                    failedEntry.State = EntityState.Detached;
                }

                progress = await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                    .SingleAsync(value => value.ModuleSessionId == moduleSession.Id, ct);
            }
        }

        if (progress is null && moduleSession is null)
        {
            // Нет активного standalone-прохождения — но это может быть идемпотентный повтор
            // запроса, который сам это прохождение завершил (например, сразу набрал 100 баллов).
            progress = await FindProgressByIdempotencyKeyAsync(command, ct);
        }

        return new ProgressResolution(progress);
    }

    /// <summary>
    /// Находит standalone-прохождение по чужой (уже не Active) резервации с тем же
    /// Idempotency-Key — единственный способ повторно найти прохождение для replay,
    /// когда клиент не передаёт progress.Id явно.
    /// </summary>
    private async Task<StudentTaskProgress?> FindProgressByIdempotencyKeyAsync(
        SubmitAttemptCommand command, CancellationToken ct)
    {
        var idempotencyKey = Guid.Parse(command.IdempotencyKey);
        var reservation = await db.AttemptReservations
            .Include(value => value.Progress).ThenInclude(value => value.ValidationVersion)
            .Where(value =>
                value.IdempotencyKey == idempotencyKey &&
                value.Progress.UserId == command.UserId &&
                value.Progress.TaskId == command.TaskId &&
                value.Progress.ModuleSessionId == null)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return reservation?.Progress;
    }
}
