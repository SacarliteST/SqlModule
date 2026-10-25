using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.PlatformIntegration.Contracts;
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
/// Выполняет отправку решения: версия проверки закрепляется в сессии,
/// а лимит и номер попытки проверяются под блокировкой владельца последовательности.
/// </summary>
public sealed class SessionAttemptWorkflow(
    IApplicationDbContext db,
    IValidationRuntimeReader runtimeReader,
    ISqlSyntaxAnalyzerResolver analyzerResolver,
    ISandboxExecutor executor,
    AttemptEvaluationEngine evaluationEngine,
    IAttemptResultSnapshotService snapshotService,
    PracticeCompletionOutbox completionOutbox,
    IOptions<SandboxOptions> sandboxOptions,
    TimeProvider timeProvider,
    ILogger<SessionAttemptWorkflow> logger) : ISubmitAttemptWorkflow
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SubmitAttemptWorkflowResult> TryHandleAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct)
    {
        var versionResult = await ResolveVersionAsync(command, moduleSession, ct);
        if (!versionResult.IsSuccess)
        {
            return Handled(Result<SubmitAttemptResponse>.Fail(versionResult.Error!));
        }

        if (!versionResult.Value.HasValue)
        {
            return new SubmitAttemptWorkflowResult(false, null);
        }

        var scope = moduleSession is null
            ? $"attempt:{command.UserId}:submit"
            : $"attempt:{command.UserId}:session:{moduleSession.Id}:submit";
        var payloadHash = HashPayload(command.TaskId, command.SubmittedSql);
        var receiptResult = await ReserveReceiptAsync(scope, command.IdempotencyKey, payloadHash, ct);
        if (!receiptResult.IsSuccess)
        {
            return Handled(Result<SubmitAttemptResponse>.Fail(receiptResult.Error!));
        }

        var receipt = receiptResult.Value!;
        var runtimeResult = await runtimeReader.ReadAsync(versionResult.Value.Value, ct);
        if (!runtimeResult.IsSuccess)
        {
            await RemoveReceiptAsync(receipt, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(runtimeResult.Error!));
        }

        var runtime = runtimeResult.Value!;
        var analyzer = analyzerResolver.Resolve(runtime.Dbms.DbmsSystemName);
        if (analyzer is null || analyzer.AnalyzerVersion != runtime.Version.AnalyzerVersion)
        {
            await RemoveReceiptAsync(receipt, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(AnalyzerUnavailable));
        }

        SqlSyntaxAnalysis analysis;
        try
        {
            analysis = analyzer.Analyze(command.SubmittedSql);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Анализатор SQL аварийно завершил проверку задания {TaskId}", command.TaskId);
            await RemoveReceiptAsync(receipt, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(AnalyzerUnavailable));
        }

        if (analysis.Status == SqlSyntaxAnalysisStatus.InfrastructureFailure)
        {
            await RemoveReceiptAsync(receipt, ct);
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
            await RemoveReceiptAsync(receipt, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Песочница аварийно завершила попытку задания {TaskId}", command.TaskId);
            await RemoveReceiptAsync(receipt, CancellationToken.None);
            return Handled(Result<SubmitAttemptResponse>.Fail(SandboxUnavailable));
        }

        if (!run.IsSuccess)
        {
            await RemoveReceiptAsync(receipt, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(SandboxUnavailable));
        }

        var finishedAt = timeProvider.GetUtcNow();
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
        var snapshot = snapshotService.Create(
            result, snapshotService.GetEffectiveRowLimit(options.MaxRows), finishedAt);
        snapshotService.Apply(attempt, snapshot);

        var saved = await PersistAsync(
            command, moduleSession, runtime, receipt, attempt, evaluation.Checks,
            evaluation.Score, snapshot, finishedAt, ct);
        return Handled(saved);
    }

    private async Task<Result<Guid?>> ResolveVersionAsync(
        SubmitAttemptCommand command,
        ModuleSession? session,
        CancellationToken ct)
    {
        if (session is null)
        {
            var versionId = await db.SqlTasks.AsNoTracking()
                .Where(value => value.Id == command.TaskId && value.PublicationStatus == PublicationStatus.Published)
                .Select(value => value.ActiveValidationVersionId)
                .SingleOrDefaultAsync(ct);
            return Result<Guid?>.Success(versionId);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockSessionAsync(session.Id, ct);
        await db.Entry(session).ReloadAsync(ct);
        var state = await CheckWritableSessionAsync(session, ct);
        if (!state.IsSuccess)
        {
            await transaction.CommitAsync(ct);
            return Result<Guid?>.Fail(state.Error!);
        }

        if (!session.ValidationVersionId.HasValue)
        {
            var versionId = await db.SqlTasks.AsNoTracking()
                .Where(value => value.Id == command.TaskId && value.PublicationStatus == PublicationStatus.Published)
                .Select(value => value.ActiveValidationVersionId)
                .SingleOrDefaultAsync(ct);
            if (!versionId.HasValue)
            {
                await transaction.RollbackAsync(ct);
                return Result<Guid?>.Fail(Error.Validation(
                    "ModuleSession.TaskValidationUnavailable",
                    "Для задания платформенной сессии не опубликована конфигурация проверки."));
            }

            session.PinValidationVersion(versionId.Value);
            await db.SaveChangesAsync(ct);
        }

        var attemptsUsed = await CountAttemptsAsync(session.Id, ct);
        var maxAttempts = session.MaxAttempts;
        if (maxAttempts.HasValue && attemptsUsed >= maxAttempts.Value)
        {
            await FinalizeSessionAsync(
                session, attemptsUsed, FinalizationReason.AttemptsExhausted,
                timeProvider.GetUtcNow(), null, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result<Guid?>.Fail(AttemptsExhausted);
        }

        await transaction.CommitAsync(ct);
        return Result<Guid?>.Success(session.ValidationVersionId);
    }

    private async Task<Result<SubmitAttemptResponse>> PersistAsync(
        SubmitAttemptCommand command,
        ModuleSession? session,
        ValidationRuntime runtime,
        MutationReceipt receipt,
        Attempt attempt,
        IReadOnlyList<EvaluatedCheck> checks,
        int score,
        AttemptResultSnapshot snapshot,
        DateTimeOffset finishedAt,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        int attemptsUsed;
        int bestScore;
        int? maxAttempts = null;
        if (session is not null)
        {
            await LockSessionAsync(session.Id, ct);
            await db.Entry(session).ReloadAsync(ct);
            var state = await CheckWritableSessionAsync(session, ct);
            if (!state.IsSuccess)
            {
                db.MutationReceipts.Remove(receipt);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Result<SubmitAttemptResponse>.Fail(state.Error!);
            }

            attemptsUsed = await CountAttemptsAsync(session.Id, ct);
            maxAttempts = session.MaxAttempts;
            if (maxAttempts.HasValue && attemptsUsed >= maxAttempts.Value)
            {
                await FinalizeSessionAsync(
                    session, attemptsUsed, FinalizationReason.AttemptsExhausted,
                    finishedAt, null, ct);
                db.MutationReceipts.Remove(receipt);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Result<SubmitAttemptResponse>.Fail(AttemptsExhausted);
            }

            bestScore = await BestScoreAsync(session.Id, ct);
        }
        else
        {
            await LockStandaloneAsync(command.UserId, command.TaskId, ct);
            attemptsUsed = await db.Attempts.CountAsync(value =>
                value.UserId == command.UserId && value.TaskId == command.TaskId &&
                value.ModuleSessionId == null && value.CountsTowardLimit, ct);
            bestScore = await db.Attempts
                .Where(value => value.UserId == command.UserId && value.TaskId == command.TaskId &&
                                value.ModuleSessionId == null && value.CountsTowardLimit)
                .MaxAsync(value => (int?)value.Score, ct) ?? 0;
        }

        var attemptNumber = session is not null
            ? await db.Attempts
                .Where(value => value.ModuleSessionId == session.Id && value.AttemptNumber != null)
                .MaxAsync(value => (int?)value.AttemptNumber, ct) ?? 0
            : await db.Attempts
                .Where(value => value.UserId == command.UserId && value.TaskId == command.TaskId &&
                                value.ModuleSessionId == null && value.AttemptNumber != null)
                .MaxAsync(value => (int?)value.AttemptNumber, ct) ?? 0;
        attemptNumber++;
        attemptsUsed++;
        bestScore = Math.Max(bestScore, score);
        attempt.AttachScoring(runtime.Version.Id, attemptNumber, score, true);
        db.Attempts.Add(attempt);
        db.AttemptCheckResults.AddRange(checks.Select(value => AttemptCheckResult.Create(
            attempt.Id, value.Check.Id, value.Check.Kind, value.Status,
            value.Check.Weight, value.AwardedScore, value.Check.Order, value.Message, null)));

        if (session is not null)
        {
            AddPracticeEvent(session, attempt, runtime.Version, checks, maxAttempts, finishedAt);
            var reason = score == 100
                ? FinalizationReason.PerfectScore
                : maxAttempts.HasValue && attemptsUsed >= maxAttempts.Value
                    ? FinalizationReason.AttemptsExhausted
                    : (FinalizationReason?)null;
            if (reason.HasValue)
            {
                await FinalizeSessionAsync(session, attemptsUsed, reason.Value, finishedAt, attempt, ct, bestScore);
            }
        }

        var response = CreateResponse(
            attempt, runtime, checks, snapshot, bestScore, attemptsUsed, maxAttempts, session, finishedAt);
        receipt.Complete(JsonSerializer.Serialize(response, JsonOptions));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation(
            "Попытка {AttemptId} задания {TaskId} оценена в {Score} баллов",
            attempt.Id, command.TaskId, score);
        return response;
    }

    private async Task<Result> CheckWritableSessionAsync(ModuleSession session, CancellationToken ct)
    {
        if (session.Status == ModuleSessionStatus.Expired)
        {
            return Result.Fail(PlatformSessionErrors.SessionExpired);
        }

        if (session.FinalizationReason == FinalizationReason.AttemptsExhausted)
        {
            return Result.Fail(AttemptsExhausted);
        }

        if (session.Status != ModuleSessionStatus.Active)
        {
            return Result.Fail(PlatformSessionErrors.SessionClosed);
        }

        var now = timeProvider.GetUtcNow();
        if (!session.IsExpired(now))
        {
            return Result.Success();
        }

        var attemptsUsed = await CountAttemptsAsync(session.Id, ct);
        await FinalizeSessionAsync(session, attemptsUsed, FinalizationReason.Expired, now, null, ct);
        session.MarkExpired(now);
        await db.SaveChangesAsync(ct);
        return Result.Fail(PlatformSessionErrors.SessionExpired);
    }

    private async Task FinalizeSessionAsync(
        ModuleSession session,
        int attemptsUsed,
        FinalizationReason reason,
        DateTimeOffset finalizedAt,
        Attempt? currentAttempt,
        CancellationToken ct,
        int? knownBestScore = null)
    {
        if (session.FinalScore.HasValue)
        {
            return;
        }

        var bestScore = knownBestScore ?? await BestScoreAsync(session.Id, ct);
        session.SetFinalResult(bestScore, reason, finalizedAt);
        await completionOutbox.TryAddAsync(
            session, bestScore, attemptsUsed, reason, finalizedAt, currentAttempt, ct);
        if (reason != FinalizationReason.Expired && session.Status == ModuleSessionStatus.Active)
        {
            session.MarkCompletionPending();
        }
    }

    private async Task<Result<MutationReceipt>> ReserveReceiptAsync(
        string scope,
        string key,
        string payloadHash,
        CancellationToken ct)
    {
        var receipt = MutationReceipt.Create(scope, key, payloadHash, String.Empty);
        db.MutationReceipts.Add(receipt);
        try
        {
            await db.SaveChangesAsync(ct);
            return receipt;
        }
        catch (DbUpdateException)
        {
            db.Entry(receipt).State = EntityState.Detached;
            var existing = await db.MutationReceipts.AsNoTracking()
                .SingleAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
            return Result<MutationReceipt>.Fail(existing.PayloadHash == payloadHash
                ? Error.Conflict("IdempotencyRequestInProgress", "Запрос с этим Idempotency-Key ещё выполняется.")
                : Error.Conflict("IdempotencyKeyPayloadMismatch", "Idempotency-Key уже использован с другим запросом."));
        }
    }

    private async Task RemoveReceiptAsync(MutationReceipt receipt, CancellationToken ct)
    {
        try
        {
            db.MutationReceipts.Remove(receipt);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Не удалось удалить незавершённую квитанцию {ReceiptId}", receipt.Id);
        }
    }

    private void AddPracticeEvent(
        ModuleSession session,
        Attempt attempt,
        TaskValidationVersion version,
        IReadOnlyList<EvaluatedCheck> checks,
        int? maxAttempts,
        DateTimeOffset occurredAt)
    {
        var visible = version.GetVisibleHintGroups().ToHashSet();
        var visibleChecks = ToVisibleChecks(checks, visible);
        var hints = ToHints(checks, visible);
        var eventId = Guid.NewGuid();
        var message = new PracticeEventMessage(
            session.Id, session.SessionKey, eventId, "sql_submit", occurredAt,
            JsonSerializer.SerializeToElement(new PracticeEventPayload(
                attempt.SubmittedSql, PracticeEventMapping.ToIntegrationStatus(attempt.Status),
                attempt.RowCount, attempt.DurationMs, attempt.IsCorrect, attempt.Reason.ToString()),
                PlatformIntegrationJson.Default),
            PracticeEventMarkdownBuilder.BuildAttempt(new PracticeAttemptReport(
                attempt.AttemptNumber, maxAttempts, attempt.Score, attempt.SubmittedSql,
                attempt.Status, attempt.IsCorrect, attempt.Reason, attempt.RowCount,
                attempt.DurationMs, AttemptMappings.ToPublicError(attempt), visibleChecks, hints)));
        db.PendingPublishes.Add(PendingPublish.Create(
            eventId, PendingPublishKind.Event, session.Id, $"event:{attempt.Id:D}",
            JsonSerializer.Serialize(message, PlatformIntegrationJson.Default), occurredAt));
    }

    private static SubmitAttemptResponse CreateResponse(
        Attempt attempt,
        ValidationRuntime runtime,
        IReadOnlyList<EvaluatedCheck> checks,
        AttemptResultSnapshot snapshot,
        int bestScore,
        int attemptsUsed,
        int? maxAttempts,
        ModuleSession? session,
        DateTimeOffset now)
    {
        var visible = runtime.Configuration.VisibleHintGroups.ToHashSet();
        int? remaining = maxAttempts.HasValue ? Math.Max(0, maxAttempts.Value - attemptsUsed) : null;
        var canSubmit = session is null || session.Status == ModuleSessionStatus.Active && remaining != 0 && !session.IsExpired(now);
        return new SubmitAttemptResponse(
            attempt.Id, attempt.Status.ToContract(), attempt.IsCorrect, attempt.Reason.ToContract(),
            attempt.RowCount, attempt.DurationMs, attempt.ErrorMessage,
            snapshot.Columns, snapshot.Rows, snapshot.IsTruncated, snapshot.State.ToContract(),
            snapshot.ReturnedRowCount, snapshot.RowLimit, snapshot.CreatedAt, snapshot.ExpiresAt,
            runtime.Version.Id, attempt.AttemptNumber, attempt.Score,
            bestScore, runtime.Version.PassingScore, bestScore >= runtime.Version.PassingScore,
            attemptsUsed, remaining, canSubmit, session?.Status == ModuleSessionStatus.Active,
            ToVisibleChecks(checks, visible), ToHints(checks, visible));
    }

    private static AttemptCheckResultResponse[] ToVisibleChecks(
        IReadOnlyList<EvaluatedCheck> checks,
        HashSet<HintGroup> visible) => checks.Where(value =>
        {
            var group = AttemptScoringReadService.ToHintGroup(value.Check.Kind);
            return group.HasValue && visible.Contains(group.Value);
        }).Select(value => new AttemptCheckResultResponse(
            value.Check.Kind.ToContract(), value.Status.ToContract(), value.Check.Weight,
            value.AwardedScore, value.Message)).ToArray();

    private static AttemptHintResponse[] ToHints(
        IReadOnlyList<EvaluatedCheck> checks,
        HashSet<HintGroup> visible) => checks
        .Where(value => value.Status == ValidationCheckStatus.Failed)
        .Select(value => AttemptScoringReadService.ToHintGroup(value.Check.Kind))
        .Where(value => value.HasValue && visible.Contains(value.Value))
        .Distinct()
        .Select(value => new AttemptHintResponse(
            value!.Value.ToContract(), "Проверьте соответствующую часть решения."))
        .ToArray();

    private async Task<int> CountAttemptsAsync(Guid sessionId, CancellationToken ct) =>
        await db.Attempts.CountAsync(value => value.ModuleSessionId == sessionId && value.CountsTowardLimit, ct);

    private async Task<int> BestScoreAsync(Guid sessionId, CancellationToken ct) =>
        await db.Attempts.Where(value => value.ModuleSessionId == sessionId && value.CountsTowardLimit)
            .MaxAsync(value => (int?)value.Score, ct) ?? 0;

    private async Task LockSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = """SELECT "SessionId" FROM "ModuleSessions" WHERE "SessionId" = @sessionId FOR UPDATE""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "sessionId";
        parameter.Value = sessionId;
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private async Task LockStandaloneAsync(Guid userId, Guid taskId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@lockKey, 0))";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lockKey";
        parameter.Value = $"attempt:{userId:D}:{taskId:D}";
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private static string HashPayload(Guid taskId, string submittedSql)
    {
        var payload = JsonSerializer.Serialize(new { TaskId = taskId, SubmittedSql = submittedSql }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static SubmitAttemptWorkflowResult Handled(Result<SubmitAttemptResponse> result) => new(true, result);

    private static Error AttemptsExhausted { get; } = Error.Conflict(
        "ModuleSession.AttemptsExhausted", "Лимит попыток платформенной сессии исчерпан.");

    private static Error AnalyzerUnavailable { get; } = Error.Unavailable(
        "Validation.AnalyzerUnavailable", "Анализатор SQL временно недоступен.");

    private static Error SandboxUnavailable { get; } = Error.Unavailable(
        "Validation.SandboxUnavailable", "Среда выполнения SQL временно недоступна.");
}
