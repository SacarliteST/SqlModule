using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.Web.Features.Training.Progress;

internal interface IProgressFinalizationService
{
    Task<Result<ProgressFinalizationResponse>> FinalizeStandaloneAsync(
        Guid userId, Guid taskId, Guid idempotencyKey, CancellationToken ct);

    Task<Result<ProgressFinalizationResponse>> FinalizePlatformAsync(
        Guid userId, Guid moduleSessionId, Guid idempotencyKey, CancellationToken ct);

    Task PrepareAsync(
        StudentTaskProgress progress,
        ModuleSession? moduleSession,
        FinalizationReason reason,
        DateTimeOffset finalizedAt,
        Attempt? currentAttempt,
        CancellationToken ct);
}

internal sealed class ProgressFinalizationService(
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<ProgressFinalizationService> logger) : IProgressFinalizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<ProgressFinalizationResponse>> FinalizeStandaloneAsync(
        Guid userId,
        Guid taskId,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var progressId = await db.StudentTaskProgresses.AsNoTracking()
            .Where(value => value.UserId == userId && value.TaskId == taskId && value.ModuleSessionId == null)
            .OrderByDescending(value => value.CreatedAt)
            .Select(value => (Guid?)value.Id)
            .FirstOrDefaultAsync(ct);
        return progressId.HasValue
            ? await FinalizeAsync(progressId.Value, userId, FinalizationReason.Manual, idempotencyKey, ct)
            : Result<ProgressFinalizationResponse>.Fail(ProgressErrors.NotFound);
    }

    public async Task<Result<ProgressFinalizationResponse>> FinalizePlatformAsync(
        Guid userId,
        Guid moduleSessionId,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var progressId = await db.StudentTaskProgresses.AsNoTracking()
            .Where(value => value.UserId == userId && value.ModuleSessionId == moduleSessionId)
            .Select(value => (Guid?)value.Id)
            .SingleOrDefaultAsync(ct);
        return progressId.HasValue
            ? await FinalizeAsync(progressId.Value, userId, FinalizationReason.Manual, idempotencyKey, ct)
            : Result<ProgressFinalizationResponse>.Fail(Error.NotFound("ModuleSession", moduleSessionId));
    }

    public async Task PrepareAsync(
        StudentTaskProgress progress,
        ModuleSession? moduleSession,
        FinalizationReason reason,
        DateTimeOffset finalizedAt,
        Attempt? currentAttempt,
        CancellationToken ct)
    {
        if (progress.Status is ProgressStatus.CompletionPending or
            ProgressStatus.CompletionFailed or ProgressStatus.Completed or ProgressStatus.Expired)
        {
            return;
        }

        progress.BeginFinalization(reason, finalizedAt);
        if (!progress.FinalScore.HasValue)
        {
            return;
        }

        if (moduleSession is null)
        {
            progress.MarkCompleted();
            return;
        }

        var deduplicationKey = $"grade:{moduleSession.Id:D}";
        var exists = db.PendingPublishes.Local.Any(value => value.DeduplicationKey == deduplicationKey) ||
                     await db.PendingPublishes.AsNoTracking()
                         .AnyAsync(value => value.DeduplicationKey == deduplicationKey, ct);
        if (!exists)
        {
            var bestAttemptId = await FindBestAttemptIdAsync(progress.Id, currentAttempt, ct);
            var completion = new PracticeCompletionRequest(
                moduleSession.SessionKey,
                progress.FinalScore.Value,
                new PracticeCompletionData(progress.AttemptsUsed, bestAttemptId),
                progress.FinalizedAt!.Value);
            db.PendingPublishes.Add(PendingPublish.Create(
                Guid.NewGuid(),
                PendingPublishKind.Grade,
                moduleSession.Id,
                deduplicationKey,
                JsonSerializer.Serialize(completion, JsonOptions),
                finalizedAt));
        }

        if (moduleSession.Status == ModuleSessionStatus.Active)
        {
            moduleSession.MarkCompletionPending();
        }

        progress.MarkCompletionPending();
    }

    private async Task<Result<ProgressFinalizationResponse>> FinalizeAsync(
        Guid progressId,
        Guid userId,
        FinalizationReason reason,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var scope = $"progress:{progressId:D}:finalize";
        var key = idempotencyKey.ToString("D");
        var hash = Hash(progressId);
        var existingReceipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
        if (existingReceipt is not null)
        {
            return Replay(existingReceipt, hash);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockProgressAsync(progressId, ct);
        existingReceipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
        if (existingReceipt is not null)
        {
            await transaction.RollbackAsync(ct);
            return Replay(existingReceipt, hash);
        }

        var progress = await db.StudentTaskProgresses
            .Include(value => value.ValidationVersion)
            .SingleOrDefaultAsync(value => value.Id == progressId && value.UserId == userId, ct);
        if (progress is null)
        {
            await transaction.RollbackAsync(ct);
            return Result<ProgressFinalizationResponse>.Fail(ProgressErrors.NotFound);
        }

        ModuleSession? moduleSession = null;
        if (progress.ModuleSessionId.HasValue)
        {
            moduleSession = await db.ModuleSessions.SingleOrDefaultAsync(
                value => value.Id == progress.ModuleSessionId.Value && value.UserId == userId, ct);
            if (moduleSession is null)
            {
                await transaction.RollbackAsync(ct);
                return Result<ProgressFinalizationResponse>.Fail(Error.NotFound(
                    "ModuleSession", progress.ModuleSessionId.Value));
            }
        }

        await PrepareAsync(progress, moduleSession, reason, timeProvider.GetUtcNow(), null, ct);
        var response = ToResponse(progress, moduleSession);
        db.MutationReceipts.Add(MutationReceipt.Create(
            scope, key, hash, JsonSerializer.Serialize(response, JsonOptions)));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Прохождение {ProgressId} финализировано с итогом {FinalScore} по причине {Reason}",
            progress.Id, progress.FinalScore, progress.FinalizationReason);
        return response;
    }

    private async Task<Guid> FindBestAttemptIdAsync(
        Guid progressId,
        Attempt? currentAttempt,
        CancellationToken ct)
    {
        var stored = await db.Attempts.AsNoTracking()
            .Where(value => value.ProgressId == progressId && value.Score != null)
            .OrderByDescending(value => value.Score)
            .ThenBy(value => value.AttemptNumber)
            .Select(value => new { value.Id, value.Score })
            .FirstOrDefaultAsync(ct);
        if (currentAttempt?.Score is { } currentScore &&
            (stored is null || currentScore > stored.Score))
        {
            return currentAttempt.Id;
        }

        return stored?.Id ?? currentAttempt?.Id ?? Guid.Empty;
    }

    private static ProgressFinalizationResponse ToResponse(
        StudentTaskProgress progress,
        ModuleSession? moduleSession) => new(
        progress.Id,
        progress.Status,
        progress.BestScore,
        progress.FinalScore ?? progress.BestScore,
        progress.FinalScore.GetValueOrDefault(progress.BestScore) >= progress.ValidationVersion.PassingScore,
        progress.FinalizationReason ?? FinalizationReason.Manual,
        progress.FinalizedAt ?? DateTimeOffset.MinValue,
        moduleSession is not null,
        moduleSession?.ReturnUrl);

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

    private static string Hash(Guid progressId) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(progressId.ToString("D"))));

    private static Result<ProgressFinalizationResponse> Replay(MutationReceipt receipt, string hash)
    {
        if (receipt.PayloadHash != hash)
        {
            return Result<ProgressFinalizationResponse>.Fail(ProgressErrors.IdempotencyPayloadMismatch);
        }

        var response = JsonSerializer.Deserialize<ProgressFinalizationResponse>(receipt.ResponseJson, JsonOptions);
        return response is null
            ? Result<ProgressFinalizationResponse>.Fail(Error.Conflict(
                "IdempotencyRequestInProgress", "Запрос с этим Idempotency-Key ещё выполняется."))
            : response;
    }
}
