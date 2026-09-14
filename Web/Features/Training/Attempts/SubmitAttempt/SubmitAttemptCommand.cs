using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.PlatformIntegration.Contracts;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.Web.Features.Training.Attempts.SubmitAttempt;

internal record SubmitAttemptCommand(
    Guid UserId,
    string StudentName,
    string? StudentEmail,
    Guid TaskId,
    string SubmittedSql,
    string IdempotencyKey,
    bool RequireModuleSession,
    Guid? ModuleSessionId)
    : IRequest<Result<SubmitAttemptResponse>>;

internal sealed class SubmitAttemptHandler(
    ITaskMaterializer materializer,
    ISandboxExecutor executor,
    IResultComparer comparer,
    IAttemptResultSnapshotService snapshotService,
    IOptions<SandboxOptions> sandboxOptions,
    TimeProvider timeProvider,
    AppDbContext db)
    : IRequestHandler<SubmitAttemptCommand, Result<SubmitAttemptResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReceiptLifetime = TimeSpan.FromHours(24);

    public async Task<Result<SubmitAttemptResponse>> Handle(
        SubmitAttemptCommand command, CancellationToken ct)
    {
        ModuleSession? moduleSession = null;
        if (command.RequireModuleSession)
        {
            if (!command.ModuleSessionId.HasValue)
            {
                return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ModuleSessionRequired);
            }

            moduleSession = await db.ModuleSessions.SingleOrDefaultAsync(
                value => value.Id == command.ModuleSessionId.Value, ct);
            if (moduleSession is null)
            {
                return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ModuleSessionRequired);
            }

            if (moduleSession.UserId != command.UserId)
            {
                return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ModuleSessionForbidden);
            }

            if (!Guid.TryParse(moduleSession.TaskRef, out var sessionTaskId) ||
                sessionTaskId != command.TaskId)
            {
                return Result<SubmitAttemptResponse>.Fail(AttemptErrors.SessionTaskMismatch);
            }
        }

        var scope = moduleSession is null
            ? $"attempt:{command.UserId}:submit"
            : $"attempt:{command.UserId}:session:{moduleSession.Id}:submit";
        var payloadHash = HashPayload(command.TaskId, command.SubmittedSql);
        var now = timeProvider.GetUtcNow();
        var existingReceipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(receipt =>
                receipt.Scope == scope && receipt.IdempotencyKey == command.IdempotencyKey, ct);
        if (existingReceipt is not null)
        {
            if (existingReceipt.CreatedAt >= now - ReceiptLifetime)
            {
                return Replay(existingReceipt, payloadHash, now);
            }

            db.MutationReceipts.Remove(existingReceipt);
            await db.SaveChangesAsync(ct);
        }

        if (moduleSession is not null &&
            (moduleSession.Status != ModuleSessionStatus.Active || moduleSession.IsExpired(now)))
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ModuleSessionClosed);
        }

        var task = await db.SqlTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == command.TaskId &&
                                      t.PublicationStatus == PublicationStatus.Published, ct);

        if (task is null)
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.TaskNotFound(command.TaskId));
        }

        var sqlQuery = await db.SqlQueries.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == task.SqlQueryId, ct);

        if (sqlQuery?.ExpectedResult is null)
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ReferenceNotReady);
        }

        var golden = GoldenResult.Deserialize(sqlQuery.ExpectedResult);
        if (golden is null)
        {
            return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ReferenceNotReady);
        }

        var mat = await materializer.MaterializeAsync(sqlQuery.TargetDbId, ct);
        if (!mat.IsSuccess)
        {
            return Result<SubmitAttemptResponse>.Fail(mat.Error!);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var receipt = MutationReceipt.Create(scope, command.IdempotencyKey, payloadHash, String.Empty);
        db.MutationReceipts.Add(receipt);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            var concurrentReceipt = await db.MutationReceipts.AsNoTracking()
                .SingleAsync(item => item.Scope == scope && item.IdempotencyKey == command.IdempotencyKey, ct);
            return Replay(concurrentReceipt, payloadHash, timeProvider.GetUtcNow());
        }

        var opts = sandboxOptions.Value;
        var snapshotRowLimit = snapshotService.GetEffectiveRowLimit(opts.MaxRows);
        var comparisonRowLimit = Math.Max(1, opts.ComparisonMaxRows);
        var startedAt = timeProvider.GetUtcNow();

        var run = await executor.RunAsync(
            mat.Value!.Dbms.ToSandboxSpec(),
            mat.Value.Setup,
            new SandboxQuery(command.SubmittedSql, opts.DefaultQueryTimeoutSeconds, comparisonRowLimit),
            ct);

        var finishedAt = timeProvider.GetUtcNow();

        if (!run.IsSuccess)
        {
            return Result<SubmitAttemptResponse>.Fail(run.Error!);
        }

        var result = run.Value!;

        ExecutionStatus status;
        CheckReason reason;
        bool isCorrect;

        if (!result.Succeeded)
        {
            var isTimeout = result.Error is not null &&
                            result.Error.Contains("timeout", StringComparison.OrdinalIgnoreCase);
            status = isTimeout ? ExecutionStatus.TimedOut : ExecutionStatus.Error;
            reason = isTimeout ? CheckReason.Timeout : CheckReason.SqlError;
            isCorrect = false;
        }
        else if (result.IsTruncated)
        {
            status = ExecutionStatus.Succeeded;
            reason = CheckReason.ResultLimitExceeded;
            isCorrect = false;
        }
        else
        {
            status = ExecutionStatus.Succeeded;
            var outcome = comparer.Compare(golden, result, sqlQuery.StrictRowOrder);
            isCorrect = outcome.IsCorrect;
            reason = outcome.Reason;
        }

        var publicError = reason == CheckReason.ResultLimitExceeded
            ? $"Результат запроса превышает безопасный лимит сравнения {comparisonRowLimit} строк."
            : result.Succeeded
                ? null
            : reason == CheckReason.Timeout
                ? "Превышено допустимое время выполнения запроса."
                : "SQL-запрос не удалось выполнить. Проверьте синтаксис и повторите попытку.";

        var attempt = Attempt.Record(
            command.UserId, command.TaskId, command.SubmittedSql,
            status, isCorrect, reason,
            result.Succeeded ? result.RowCount : null,
            result.Succeeded ? result.DurationMs : null,
            publicError,
            startedAt, finishedAt,
            studentName: command.StudentName,
            studentEmail: command.StudentEmail,
            moduleSessionId: moduleSession?.Id);

        var snapshot = snapshotService.Create(result, snapshotRowLimit, finishedAt);
        snapshotService.Apply(attempt, snapshot);

        var response = new SubmitAttemptResponse(
            attempt.Id, status, isCorrect, reason,
            result.Succeeded ? result.RowCount : null,
            result.Succeeded ? result.DurationMs : null,
            publicError,
            snapshot.Columns, snapshot.Rows, snapshot.IsTruncated,
            snapshot.State, snapshot.ReturnedRowCount, snapshot.RowLimit,
            snapshot.CreatedAt, snapshot.ExpiresAt);

        db.Attempts.Add(attempt);
        if (moduleSession is not null)
        {
            var eventId = Guid.NewGuid();
            var message = new PracticeEventMessage(
                moduleSession.Id,
                moduleSession.SessionKey,
                eventId,
                "sql_submit",
                finishedAt,
                new PracticeEventPayload(
                    command.SubmittedSql,
                    ToIntegrationStatus(status),
                    result.Succeeded ? result.RowCount : null,
                    result.Succeeded ? result.DurationMs : null,
                    isCorrect,
                    reason.ToString()));
            db.PendingPublishes.Add(PendingPublish.Create(
                eventId,
                PendingPublishKind.Event,
                moduleSession.Id,
                $"event:{attempt.Id:D}",
                JsonSerializer.Serialize(message, JsonOptions),
                finishedAt));

            if (isCorrect)
            {
                var previousAttempts = await db.Attempts.CountAsync(
                    value => value.ModuleSessionId == moduleSession.Id, ct);
                var completion = new PracticeCompletionRequest(
                    moduleSession.SessionKey,
                    100,
                    new PracticeCompletionData(previousAttempts + 1, attempt.Id),
                    finishedAt);
                db.PendingPublishes.Add(PendingPublish.Create(
                    Guid.NewGuid(),
                    PendingPublishKind.Grade,
                    moduleSession.Id,
                    $"grade:{moduleSession.Id:D}",
                    JsonSerializer.Serialize(completion, JsonOptions),
                    finishedAt));
                moduleSession.MarkCompletionPending();
            }
        }

        receipt.Complete(JsonSerializer.Serialize(response, JsonOptions));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (moduleSession is not null && isCorrect)
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            var gradeExists = await db.PendingPublishes.AsNoTracking().AnyAsync(
                value => value.DeduplicationKey == $"grade:{moduleSession.Id:D}", ct);
            if (gradeExists)
            {
                return Result<SubmitAttemptResponse>.Fail(AttemptErrors.ModuleSessionClosed);
            }

            throw;
        }

        await transaction.CommitAsync(ct);
        if (moduleSession is not null && isCorrect)
        {
            ModuleIntegrationTelemetry.RecordSessionTransition(
                ModuleSessionStatus.Active,
                ModuleSessionStatus.CompletionPending);
        }

        return response;
    }

    private static string ToIntegrationStatus(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "SUCCESS",
        ExecutionStatus.Error => "ERROR",
        ExecutionStatus.TimedOut => "TIMEOUT",
        _ => "UNKNOWN"
    };

    private static string HashPayload(Guid taskId, string submittedSql)
    {
        var payload = JsonSerializer.Serialize(new { TaskId = taskId, SubmittedSql = submittedSql }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static Result<SubmitAttemptResponse> Replay(
        MutationReceipt receipt, string payloadHash, DateTimeOffset now)
    {
        if (receipt.PayloadHash != payloadHash)
        {
            return Result<SubmitAttemptResponse>.Fail(Error.Conflict(
                "IdempotencyKeyPayloadMismatch", "Idempotency-Key уже использован с другим запросом."));
        }

        var response = JsonSerializer.Deserialize<SubmitAttemptResponse>(receipt.ResponseJson, JsonOptions);
        if (response is null)
        {
            return Result<SubmitAttemptResponse>.Fail(Error.Conflict(
                "IdempotencyRequestInProgress", "Запрос с этим Idempotency-Key ещё выполняется."));
        }

        if (response.ResultSnapshotState == AttemptResultSnapshotState.Available &&
            response.ResultSnapshotExpiresAt.HasValue &&
            response.ResultSnapshotExpiresAt.Value <= now)
        {
            response = response with
            {
                ResultSnapshotState = AttemptResultSnapshotState.Expired,
                ActualColumns = null,
                ActualRows = null,
                ReturnedRowCount = null,
                IsResultTruncated = false
            };
        }

        return response;
    }
}
