using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Application.Training.Progress;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

public sealed record SubmitAttemptWorkflowResult(bool Handled, Result<SubmitAttemptResponse>? Result);

public interface ISubmitAttemptWorkflow
{
    Task<SubmitAttemptWorkflowResult> TryHandleAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct);
}

/// <summary>
/// Оркестратор отправки решения по версии проверки: прохождение → резервация номера → runtime версии →
/// анализ и выполнение SQL → оценка → атомарное сохранение. Каждый шаг вынесен в отдельный сервис.
/// </summary>
public sealed class SubmitAttemptWorkflow(
    IApplicationDbContext db,
    IAttemptProgressResolver progressResolver,
    IAttemptReservationService reservationService,
    IAttemptPersistenceService persistence,
    IPlatformStudentScope platformScope,
    IValidationRuntimeReader runtimeReader,
    ISqlSyntaxAnalyzerResolver analyzerResolver,
    ISandboxExecutor executor,
    AttemptEvaluationEngine evaluationEngine,
    IAttemptResultSnapshotService snapshotService,
    IOptions<SandboxOptions> sandboxOptions,
    TimeProvider timeProvider,
    ILogger<SubmitAttemptWorkflow> logger) : ISubmitAttemptWorkflow
{
    public async Task<SubmitAttemptWorkflowResult> TryHandleAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct)
    {
        var resolution = await progressResolver.ResolveAsync(command, moduleSession, ct);
        if (resolution.IsFailure)
        {
            return Handled(Result<SubmitAttemptResponse>.Fail(resolution.Error!));
        }

        var progress = resolution.Progress;
        if (progress is null)
        {
            return new SubmitAttemptWorkflowResult(false, null);
        }

        var payloadHash = HashPayload(command.TaskId, command.SubmittedSql);
        var reservationResult = await reservationService.ReserveAsync(
            progress.Id, Guid.Parse(command.IdempotencyKey), payloadHash, ct);
        if (!reservationResult.IsSuccess)
        {
            var error = reservationResult.Error!;
            if (moduleSession is not null && error.Code == ProgressErrors.Closed.Code)
            {
                // Прохождение закрыто — отличаем причину на стороне сессии (истекла/завершена);
                // если сессия сама ещё активна, остаётся Progress.Closed (гонка двух попыток).
                var sessionState = await platformScope.ResolveAsync(
                    command.UserId, moduleSession.Id, PlatformScopeMode.Write, ct);
                if (!sessionState.IsSuccess)
                {
                    error = sessionState.Error!;
                }
            }

            return Handled(Result<SubmitAttemptResponse>.Fail(error));
        }

        var reservation = reservationResult.Value!.Reservation;
        if (reservationResult.Value.IsReplay)
        {
            if (reservation.State != AttemptReservationState.Completed || !reservation.AttemptId.HasValue)
            {
                return Handled(Result<SubmitAttemptResponse>.Fail(Error.Conflict(
                    "IdempotencyRequestInProgress",
                    "Запрос с этим Idempotency-Key ещё выполняется.")));
            }

            return Handled(await ReplayAsync(reservation.AttemptId.Value, ct));
        }

        // ReserveAsync при конкурентной гонке за номер попытки мог внутри своего retry
        // сделать db.ChangeTracker.Clear() и заново отследить прогресс новым CLR-инстансом —
        // подхватываем именно его, чтобы не держать вторую, уже отсоединённую ссылку на тот
        // же Id (иначе db.Entry(progress).ReloadAsync упадёт с "another instance ...
        // already tracked").
        progress = db.ChangeTracker.Entries<StudentTaskProgress>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(entity => entity.Id == progress.Id) ?? progress;

        var runtimeResult = await runtimeReader.ReadAsync(progress.ValidationVersionId, ct);
        if (!runtimeResult.IsSuccess)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(runtimeResult.Error!));
        }

        var runtime = runtimeResult.Value!;
        var analyzer = analyzerResolver.Resolve(runtime.Dbms.DbmsSystemName);
        if (analyzer is null || analyzer.AnalyzerVersion != runtime.Version.AnalyzerVersion)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(AnalyzerUnavailable));
        }

        SqlSyntaxAnalysis analysis;
        try
        {
            analysis = analyzer.Analyze(command.SubmittedSql);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Анализатор SQL аварийно завершил проверку прохождения {ProgressId}",
                progress.Id);
            await persistence.ReleaseReservationSafelyAsync(reservation.Id);
            return Handled(Result<SubmitAttemptResponse>.Fail(AnalyzerUnavailable));
        }

        if (analysis.Status == SqlSyntaxAnalysisStatus.InfrastructureFailure)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(AnalyzerUnavailable));
        }

        var options = sandboxOptions.Value;
        var comparisonRowLimit = Math.Max(1, options.ComparisonMaxRows);
        var startedAt = timeProvider.GetUtcNow();
        Result<QueryResultSet> run;
        try
        {
            run = await executor.RunAsync(
                runtime.Dbms.ToSandboxSpec(),
                runtime.Setup,
                new SandboxQuery(command.SubmittedSql, options.DefaultQueryTimeoutSeconds, comparisonRowLimit),
                ct);
        }
        catch (OperationCanceledException)
        {
            await persistence.ReleaseReservationSafelyAsync(reservation.Id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Песочница аварийно завершила попытку прохождения {ProgressId}",
                progress.Id);
            await persistence.ReleaseReservationSafelyAsync(reservation.Id);
            return Handled(Result<SubmitAttemptResponse>.Fail(SandboxUnavailable));
        }

        var finishedAt = timeProvider.GetUtcNow();
        if (!run.IsSuccess)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(SandboxUnavailable));
        }

        var result = run.Value!;
        var evaluation = evaluationEngine.Evaluate(result, analysis, new AttemptEvaluationInput(
            runtime.Expected,
            runtime.Reference.IsRequiredRowOrder,
            runtime.Configuration.Checks,
            runtime.Schema.Tables.ToDictionary(value => value.Key, value => value.Name),
            comparisonRowLimit));
        var main = evaluation.Main;
        var attempt = Attempt.Record(
            command.UserId, command.TaskId, command.SubmittedSql,
            main.Status, main.IsCorrect, main.Reason,
            result.Succeeded ? result.RowCount : null,
            result.Succeeded ? result.DurationMs : null,
            main.PublicError, startedAt, finishedAt,
            studentName: command.StudentName,
            studentEmail: command.StudentEmail,
            moduleSessionId: moduleSession?.Id);
        attempt.AttachScoring(progress.Id, runtime.Version.Id, reservation.AttemptNumber, evaluation.Score, true);
        var snapshot = snapshotService.Create(
            result, snapshotService.GetEffectiveRowLimit(options.MaxRows), finishedAt);
        snapshotService.Apply(attempt, snapshot);

        var saved = await persistence.SaveAsync(
            new AttemptPersistenceRequest(
                progress, moduleSession, runtime.Version, reservation, attempt,
                evaluation.Checks, evaluation.Score, finishedAt),
            ct);
        if (!saved.IsSuccess)
        {
            return Handled(Result<SubmitAttemptResponse>.Fail(saved.Error!));
        }

        logger.LogInformation(
            "Попытка {AttemptId} прохождения {ProgressId} оценена в {Score} баллов",
            attempt.Id, progress.Id, evaluation.Score);

        return Handled(SubmitAttemptResponseFactory.Create(
            attempt, progress, runtime, evaluation.Checks, snapshot, finishedAt));
    }

    private static Error AnalyzerUnavailable { get; } = Error.Unavailable(
        "Validation.AnalyzerUnavailable",
        "Анализатор SQL временно недоступен.");

    private static Error SandboxUnavailable { get; } = Error.Unavailable(
        "Validation.SandboxUnavailable",
        "Среда выполнения SQL временно недоступна.");

    private async Task<Result<SubmitAttemptResponse>> ReplayAsync(Guid attemptId, CancellationToken ct)
    {
        var attempt = await db.Attempts.AsNoTracking().SingleAsync(value => value.Id == attemptId, ct);
        var progress = await db.StudentTaskProgresses.AsNoTracking()
            .SingleAsync(value => value.Id == attempt.ProgressId, ct);
        var runtimeResult = await runtimeReader.ReadAsync(attempt.ValidationVersionId!.Value, ct);
        if (!runtimeResult.IsSuccess)
        {
            return Result<SubmitAttemptResponse>.Fail(runtimeResult.Error!);
        }

        var storedChecks = await db.AttemptCheckResults.AsNoTracking()
            .Where(value => value.AttemptId == attemptId)
            .OrderBy(value => value.Order)
            .ToArrayAsync(ct);
        var runtime = runtimeResult.Value!;
        var byId = runtime.Configuration.Checks.ToDictionary(value => value.Id);
        var checks = storedChecks.Select(value => new EvaluatedCheck(
            byId[value.ValidationCheckId], value.Status, value.AwardedScore, value.Message)).ToArray();
        return SubmitAttemptResponseFactory.Create(
            attempt, progress, runtime, checks,
            snapshotService.Read(attempt, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
    }

    private static string HashPayload(Guid taskId, string submittedSql) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{taskId:D}\n{submittedSql}")));

    private static SubmitAttemptWorkflowResult Handled(Result<SubmitAttemptResponse> result) => new(true, result);
}
