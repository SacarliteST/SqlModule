using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;

namespace SQLModule.Application.ModuleIntegration;

public interface IModuleSessionFinalizationService
{
    Task<Result<SessionFinalizationResponse>> FinalizeAsync(
        Guid userId,
        Guid sessionId,
        Guid idempotencyKey,
        CancellationToken ct);
}

public sealed class ModuleSessionFinalizationService(
    IApplicationDbContext db,
    PracticeCompletionOutbox completionOutbox,
    TimeProvider timeProvider,
    ILogger<ModuleSessionFinalizationService> logger) : IModuleSessionFinalizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<SessionFinalizationResponse>> FinalizeAsync(
        Guid userId,
        Guid sessionId,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var scope = $"session:{sessionId:D}:finalize";
        var key = idempotencyKey.ToString("D");
        var hash = Hash(sessionId);
        var existing = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
        if (existing is not null)
        {
            return Replay(existing, hash);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockSessionAsync(sessionId, ct);
        existing = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Scope == scope && value.IdempotencyKey == key, ct);
        if (existing is not null)
        {
            await transaction.RollbackAsync(ct);
            return Replay(existing, hash);
        }

        var session = await db.ModuleSessions.SingleOrDefaultAsync(
            value => value.Id == sessionId && value.UserId == userId, ct);
        if (session is null)
        {
            await transaction.RollbackAsync(ct);
            return Result<SessionFinalizationResponse>.Fail(PlatformSessionErrors.SessionNotFound);
        }

        if (session.Status != ModuleSessionStatus.Active &&
            (!session.FinalScore.HasValue || !session.FinalizationReason.HasValue || !session.FinalizedAt.HasValue))
        {
            await transaction.RollbackAsync(ct);
            return Result<SessionFinalizationResponse>.Fail(PlatformSessionErrors.SessionClosed);
        }

        if (session.Status != ModuleSessionStatus.Active)
        {
            var completedResponse = await CreateResponseAsync(session, ct);
            db.MutationReceipts.Add(MutationReceipt.Create(
                scope, key, hash, JsonSerializer.Serialize(completedResponse, JsonOptions)));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return completedResponse;
        }

        var attemptsUsed = await db.Attempts.CountAsync(value =>
            value.ModuleSessionId == sessionId && value.CountsTowardLimit, ct);
        var bestScore = await db.Attempts
            .Where(value => value.ModuleSessionId == sessionId && value.CountsTowardLimit)
            .MaxAsync(value => (int?)value.Score, ct) ?? 0;
        var finalizedAt = timeProvider.GetUtcNow();
        var versionId = session.ValidationVersionId;
        if (!versionId.HasValue && Guid.TryParse(session.TaskRef, out var taskId))
        {
            versionId = await db.SqlTasks.AsNoTracking()
                .Where(value => value.Id == taskId)
                .Select(value => value.ActiveValidationVersionId)
                .SingleOrDefaultAsync(ct);
            if (versionId.HasValue)
            {
                session.PinValidationVersion(versionId.Value);
            }
        }

        var reason = session.IsExpired(finalizedAt)
            ? FinalizationReason.Expired
            : FinalizationReason.Manual;
        session.SetFinalResult(bestScore, reason, finalizedAt);
        await completionOutbox.TryAddAsync(
            session, bestScore, attemptsUsed, reason, finalizedAt, null, ct);
        if (reason == FinalizationReason.Expired)
        {
            session.MarkExpired(finalizedAt);
        }
        else
        {
            session.MarkCompletionPending();
        }

        var response = await CreateResponseAsync(session, ct);
        db.MutationReceipts.Add(MutationReceipt.Create(
            scope, key, hash, JsonSerializer.Serialize(response, JsonOptions)));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation(
            "Платформенная сессия {SessionId} вручную завершена с итогом {FinalScore}",
            sessionId, bestScore);
        return response;
    }

    private async Task<SessionFinalizationResponse> CreateResponseAsync(
        ModuleSession session,
        CancellationToken ct)
    {
        var versionId = session.ValidationVersionId;
        if (!versionId.HasValue && Guid.TryParse(session.TaskRef, out var taskId))
        {
            versionId = await db.SqlTasks.AsNoTracking()
                .Where(value => value.Id == taskId)
                .Select(value => value.ActiveValidationVersionId)
                .SingleOrDefaultAsync(ct);
        }

        var passingScore = versionId.HasValue
            ? await db.TaskValidationVersions.AsNoTracking()
                .Where(value => value.Id == versionId.Value)
                .Select(value => value.PassingScore)
                .SingleOrDefaultAsync(ct)
            : 100;
        var finalScore = session.FinalScore ?? 0;
        return new SessionFinalizationResponse(
            session.Status.ToContract(),
            finalScore,
            finalScore,
            finalScore >= passingScore,
            (session.FinalizationReason ?? FinalizationReason.Manual).ToContract(),
            session.FinalizedAt ?? timeProvider.GetUtcNow(),
            true,
            session.ReturnUrl);
    }

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

    private static string Hash(Guid sessionId) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(sessionId.ToString("D"))));

    private static Result<SessionFinalizationResponse> Replay(MutationReceipt receipt, string hash)
    {
        if (receipt.PayloadHash != hash)
        {
            return Result<SessionFinalizationResponse>.Fail(Error.Conflict(
                "IdempotencyKeyPayloadMismatch", "Idempotency-Key уже использован с другим запросом."));
        }

        var response = JsonSerializer.Deserialize<SessionFinalizationResponse>(receipt.ResponseJson, JsonOptions);
        return response is null
            ? Result<SessionFinalizationResponse>.Fail(Error.Conflict(
                "IdempotencyRequestInProgress", "Запрос с этим Idempotency-Key ещё выполняется."))
            : response;
    }
}
