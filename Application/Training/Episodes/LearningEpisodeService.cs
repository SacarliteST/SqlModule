using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Episode;
using SQLModule.Domain.Common;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Episodes;

public interface ILearningEpisodeService
{
    Task<Result<LearningEpisodeResponse>> StartAsync(
        Guid studentId, Guid taskId, Guid? moduleSessionId, CancellationToken ct);

    Task<Result<LearningEpisodeResponse>> HeartbeatAsync(
        Guid studentId, Guid episodeId, CancellationToken ct);

    Task<Result<LearningEpisodeResponse>> FinalizeAsync(
        Guid studentId,
        Guid episodeId,
        LearningEpisodeFinalizationMode mode,
        Guid idempotencyKey,
        CancellationToken ct);
}

public static class LearningEpisodeErrors
{
    public static Error NotFound { get; } = Error.NotFound(
        "LearningEpisode.NotFound", "Учебный эпизод не найден или недоступен.");

    public static Error TaskUnavailable { get; } = Error.NotFound(
        "LearningEpisode.TaskUnavailable", "Опубликованное задание недоступно.");

    public static Error Finalized { get; } = Error.Conflict(
        "LearningEpisode.Finalized", "Учебный эпизод уже финализирован.");

    public static Error HeartbeatExpired { get; } = Error.Conflict(
        "LearningEpisode.HeartbeatExpired", "Окно восстановления учебного эпизода истекло.");

    public static Error AttemptRequired { get; } = Error.Validation(
        "LearningEpisode.AttemptRequired", "Нельзя принять частичный результат без попытки.");
}

public sealed class LearningEpisodeService(
    IApplicationDbContext db,
    TimeProvider timeProvider,
    ILogger<LearningEpisodeService> logger) : ILearningEpisodeService
{
    public static readonly TimeSpan HeartbeatGracePeriod = TimeSpan.FromMinutes(3);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal async Task<Result<LearningEpisode>> GetOrStartForAttemptAsync(
        Guid studentId,
        Guid taskId,
        Guid validationVersionId,
        Guid? moduleSessionId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var episode = moduleSessionId.HasValue
            ? await db.LearningEpisodes.SingleOrDefaultAsync(
                value => value.ModuleSessionId == moduleSessionId.Value, ct)
            : await db.LearningEpisodes.SingleOrDefaultAsync(value =>
                value.StudentId == studentId &&
                value.TaskId == taskId &&
                value.ModuleSessionId == null &&
                value.Status == LearningEpisodeStatus.Active, ct);
        if (episode is { Status: LearningEpisodeStatus.Active } &&
            episode.LastHeartbeatAt + HeartbeatGracePeriod <= now)
        {
            await FinalizeEntityAsync(
                episode, LearningEpisodeFinalizationReason.HeartbeatTimeout, now, ct);
            if (moduleSessionId.HasValue)
            {
                return Result<LearningEpisode>.Fail(LearningEpisodeErrors.HeartbeatExpired);
            }

            episode = null;
        }

        if (episode is not null)
        {
            if (episode.Status == LearningEpisodeStatus.Finalized)
            {
                return Result<LearningEpisode>.Fail(LearningEpisodeErrors.Finalized);
            }

            episode.Heartbeat(now);
            return episode;
        }

        var created = LearningEpisode.Start(
            studentId, taskId, validationVersionId, moduleSessionId, now);
        db.LearningEpisodes.Add(created);
        return created;
    }

    internal Task<Result> FinalizeAfterAttemptAsync(
        LearningEpisode episode,
        LearningEpisodeFinalizationReason reason,
        Attempt currentAttempt,
        DateTimeOffset finalizedAt,
        CancellationToken ct) =>
        FinalizeEntityAsync(
            episode,
            reason,
            finalizedAt,
            ct,
            reason == LearningEpisodeFinalizationReason.PerfectScore ? currentAttempt : null);

    internal async Task FinalizeInfrastructureFailureAsync(
        Guid studentId,
        Guid taskId,
        Guid? moduleSessionId,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOwnerAsync(studentId, taskId, moduleSessionId, ct);
        var episode = await db.LearningEpisodes.SingleOrDefaultAsync(value =>
            value.StudentId == studentId &&
            value.TaskId == taskId &&
            value.ModuleSessionId == moduleSessionId &&
            value.Status == LearningEpisodeStatus.Active, ct);
        if (episode is not null)
        {
            await FinalizeEntityAsync(
                episode,
                LearningEpisodeFinalizationReason.InfrastructureFailure,
                timeProvider.GetUtcNow(),
                ct);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task<Result<LearningEpisodeResponse>> StartAsync(
        Guid studentId,
        Guid taskId,
        Guid? moduleSessionId,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockOwnerAsync(studentId, taskId, moduleSessionId, ct);

        var versionId = await db.SqlTasks.AsNoTracking()
            .Where(task =>
                task.Id == taskId &&
                task.PublicationStatus == PublicationStatus.Published &&
                task.ActiveValidationVersionId != null)
            .Select(task => task.ActiveValidationVersionId)
            .SingleOrDefaultAsync(ct);
        if (!versionId.HasValue)
        {
            await transaction.RollbackAsync(ct);
            return Result<LearningEpisodeResponse>.Fail(LearningEpisodeErrors.TaskUnavailable);
        }

        if (moduleSessionId.HasValue)
        {
            var session = await db.ModuleSessions.SingleOrDefaultAsync(session =>
                session.Id == moduleSessionId.Value && session.UserId == studentId, ct);
            if (session is null ||
                session.Status != ModuleSessionStatus.Active ||
                session.IsExpired(now) ||
                !Guid.TryParse(session.TaskRef, out var sessionTaskId) ||
                sessionTaskId != taskId)
            {
                await transaction.RollbackAsync(ct);
                return Result<LearningEpisodeResponse>.Fail(LearningEpisodeErrors.TaskUnavailable);
            }

            session.PinValidationVersion(versionId.Value);
            versionId = session.ValidationVersionId;
        }

        var episode = moduleSessionId.HasValue
            ? await db.LearningEpisodes.SingleOrDefaultAsync(
                value => value.ModuleSessionId == moduleSessionId.Value, ct)
            : await db.LearningEpisodes.SingleOrDefaultAsync(value =>
                value.StudentId == studentId &&
                value.TaskId == taskId &&
                value.ModuleSessionId == null &&
                value.Status == LearningEpisodeStatus.Active, ct);

        if (episode is not null)
        {
            if (episode.Status == LearningEpisodeStatus.Finalized)
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ToResponse(episode);
            }

            if (episode.LastHeartbeatAt + HeartbeatGracePeriod > now)
            {
                episode.Heartbeat(now);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ToResponse(episode);
            }

            await FinalizeEntityAsync(
                episode, LearningEpisodeFinalizationReason.HeartbeatTimeout, now, ct);
            if (moduleSessionId.HasValue)
            {
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ToResponse(episode);
            }
        }

        var effectiveVersionId = versionId ?? throw new InvalidOperationException(
            "Для учебного эпизода не определена версия проверки.");
        var created = LearningEpisode.Start(
            studentId, taskId, effectiveVersionId, moduleSessionId, now);
        db.LearningEpisodes.Add(created);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation(
            "Начат учебный эпизод {EpisodeId} для задания {TaskId}",
            created.Id,
            taskId);
        return ToResponse(created);
    }

    public async Task<Result<LearningEpisodeResponse>> HeartbeatAsync(
        Guid studentId,
        Guid episodeId,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockEpisodeAsync(episodeId, ct);
        var episode = await db.LearningEpisodes.SingleOrDefaultAsync(value =>
            value.Id == episodeId && value.StudentId == studentId, ct);
        if (episode is null)
        {
            await transaction.RollbackAsync(ct);
            return Result<LearningEpisodeResponse>.Fail(LearningEpisodeErrors.NotFound);
        }

        if (episode.Status == LearningEpisodeStatus.Finalized)
        {
            await transaction.RollbackAsync(ct);
            return Result<LearningEpisodeResponse>.Fail(LearningEpisodeErrors.Finalized);
        }

        if (episode.LastHeartbeatAt + HeartbeatGracePeriod <= now)
        {
            await FinalizeEntityAsync(
                episode, LearningEpisodeFinalizationReason.HeartbeatTimeout, now, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result<LearningEpisodeResponse>.Fail(LearningEpisodeErrors.HeartbeatExpired);
        }

        episode.Heartbeat(now);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return ToResponse(episode);
    }

    public async Task<Result<LearningEpisodeResponse>> FinalizeAsync(
        Guid studentId,
        Guid episodeId,
        LearningEpisodeFinalizationMode mode,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var receiptScope = $"episode:{episodeId:D}:finalize";
        var receiptKey = idempotencyKey.ToString("D");
        var payloadHash = Hash(mode.ToString());
        var existingReceipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.Scope == receiptScope && value.IdempotencyKey == receiptKey, ct);
        if (existingReceipt is not null)
        {
            return Replay(existingReceipt, payloadHash);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockEpisodeAsync(episodeId, ct);
        var episode = await db.LearningEpisodes.SingleOrDefaultAsync(value =>
            value.Id == episodeId && value.StudentId == studentId, ct);
        if (episode is null)
        {
            await transaction.RollbackAsync(ct);
            return Result<LearningEpisodeResponse>.Fail(LearningEpisodeErrors.NotFound);
        }

        var receiptAfterLock = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.Scope == receiptScope && value.IdempotencyKey == receiptKey, ct);
        if (receiptAfterLock is not null)
        {
            await transaction.RollbackAsync(ct);
            return Replay(receiptAfterLock, payloadHash);
        }

        if (episode.Status == LearningEpisodeStatus.Active)
        {
            var reason = mode == LearningEpisodeFinalizationMode.AcceptPartialResult
                ? LearningEpisodeFinalizationReason.AcceptedPartialResult
                : LearningEpisodeFinalizationReason.Abandoned;
            var finalized = await FinalizeEntityAsync(episode, reason, timeProvider.GetUtcNow(), ct);
            if (!finalized.IsSuccess)
            {
                await transaction.RollbackAsync(ct);
                return Result<LearningEpisodeResponse>.Fail(finalized.Error!);
            }
        }

        var response = ToResponse(episode);
        db.MutationReceipts.Add(MutationReceipt.Create(
            receiptScope,
            receiptKey,
            payloadHash,
            JsonSerializer.Serialize(response, JsonOptions)));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return response;
    }

    internal async Task<Result> FinalizeEntityAsync(
        LearningEpisode episode,
        LearningEpisodeFinalizationReason reason,
        DateTimeOffset finalizedAt,
        CancellationToken ct,
        Attempt? preferredAttempt = null)
    {
        if (episode.Status == LearningEpisodeStatus.Finalized)
        {
            return Result.Success();
        }

        var finalAttempt = preferredAttempt ?? await db.Attempts.AsNoTracking()
            .Where(attempt =>
                attempt.LearningEpisodeId == episode.Id &&
                attempt.CountsTowardLimit &&
                attempt.Score != null)
            .OrderByDescending(attempt => attempt.Score)
            .ThenBy(attempt => attempt.FinishedAt)
            .ThenBy(attempt => attempt.Id)
            .FirstOrDefaultAsync(ct);
        if (reason == LearningEpisodeFinalizationReason.AcceptedPartialResult && finalAttempt is null)
        {
            return Result.Fail(LearningEpisodeErrors.AttemptRequired);
        }

        var skillResults = await CreateSkillResultsAsync(
            episode, finalAttempt, reason, ct);
        episode.Finalize(reason, finalizedAt, finalAttempt?.Id, skillResults);
        db.LearningEpisodeSkillResults.AddRange(skillResults);
        if (skillResults.Count == 0 && CreatesPedagogicalObservation(reason))
        {
            logger.LogWarning(
                "Учебный эпизод {EpisodeId} закрыт без проверяемых результатов навыков",
                episode.Id);
        }

        return Result.Success();
    }

    private async Task<IReadOnlyList<LearningEpisodeSkillResult>> CreateSkillResultsAsync(
        LearningEpisode episode,
        Attempt? finalAttempt,
        LearningEpisodeFinalizationReason reason,
        CancellationToken ct)
    {
        if (!CreatesPedagogicalObservation(reason))
        {
            return [];
        }

        var snapshotJson = await db.TaskValidationVersions.AsNoTracking()
            .Where(version => version.Id == episode.ValidationVersionId)
            .Select(version => version.ValidationConfigurationSnapshotJson)
            .SingleAsync(ct);
        var snapshot = JsonSerializer.Deserialize<ValidationSkillSnapshot>(snapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Снимок навыков опубликованной версии повреждён.");
        var results = new List<LearningEpisodeSkillResult>();

        if (finalAttempt is not null)
        {
            var checkResults = await db.AttemptCheckResults.AsNoTracking()
                .Where(result => result.AttemptId == finalAttempt.Id)
                .ToDictionaryAsync(result => result.ValidationCheckId, ct);
            foreach (var check in snapshot.Checks ?? [])
            {
                if (check.Skill is null || !checkResults.TryGetValue(check.Id, out var result))
                {
                    continue;
                }

                results.Add(LearningEpisodeSkillResult.Create(
                    episode.Id,
                    check.Skill.Id,
                    check.Skill.SkillId,
                    LearningEpisodeSkillSource.ValidationRule,
                    result.Status == ValidationCheckStatus.Passed
                        ? LearningEpisodeSkillOutcome.Succeeded
                        : LearningEpisodeSkillOutcome.Failed));
            }
        }

        var tagOutcome = reason switch
        {
            LearningEpisodeFinalizationReason.PerfectScore => LearningEpisodeSkillOutcome.Succeeded,
            LearningEpisodeFinalizationReason.AcceptedPartialResult => (LearningEpisodeSkillOutcome?)null,
            _ => LearningEpisodeSkillOutcome.Failed
        };
        if (tagOutcome.HasValue)
        {
            results.AddRange((snapshot.SkillTags ?? []).Select(tag => LearningEpisodeSkillResult.Create(
                episode.Id,
                tag.Id,
                tag.SkillId,
                LearningEpisodeSkillSource.TaskTag,
                tagOutcome.Value)));
        }

        return results;
    }

    private static bool CreatesPedagogicalObservation(LearningEpisodeFinalizationReason reason) => reason is
        LearningEpisodeFinalizationReason.PerfectScore or
        LearningEpisodeFinalizationReason.AcceptedPartialResult or
        LearningEpisodeFinalizationReason.Abandoned or
        LearningEpisodeFinalizationReason.AttemptsExhausted or
        LearningEpisodeFinalizationReason.HeartbeatTimeout or
        LearningEpisodeFinalizationReason.LearningTimeExpired;

    private async Task LockOwnerAsync(
        Guid studentId,
        Guid taskId,
        Guid? moduleSessionId,
        CancellationToken ct) =>
        await AdvisoryLockAsync(moduleSessionId?.ToString("D") ?? $"{studentId:D}:{taskId:D}", ct);

    private async Task LockEpisodeAsync(Guid episodeId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = """SELECT "Id" FROM "LearningEpisodes" WHERE "Id" = @episodeId FOR UPDATE""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "episodeId";
        parameter.Value = episodeId;
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private async Task AdvisoryLockAsync(string value, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@value, 0))";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "value";
        parameter.Value = value;
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private static LearningEpisodeResponse ToResponse(LearningEpisode episode) => new(
        episode.Id,
        episode.TaskId,
        episode.ValidationVersionId,
        (LearningEpisodeStatusResponse)episode.Status,
        episode.StartedAt,
        episode.LastHeartbeatAt,
        episode.FinalizedAt,
        episode.FinalAttemptId,
        episode.ProducesObservation);

    private static string Hash(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static Result<LearningEpisodeResponse> Replay(MutationReceipt receipt, string payloadHash)
    {
        if (receipt.PayloadHash != payloadHash)
        {
            return Result<LearningEpisodeResponse>.Fail(Error.Conflict(
                "IdempotencyKeyPayloadMismatch",
                "Idempotency-Key уже использован с другим запросом."));
        }

        var response = JsonSerializer.Deserialize<LearningEpisodeResponse>(receipt.ResponseJson, JsonOptions);
        return response is null
            ? Result<LearningEpisodeResponse>.Fail(Error.Conflict(
                "IdempotencyRequestInProgress",
                "Запрос с этим Idempotency-Key ещё выполняется."))
            : response;
    }

    private sealed record ValidationSkillSnapshot(
        IReadOnlyList<ValidationCheckSkillSnapshot>? Checks,
        IReadOnlyList<SkillSnapshot>? SkillTags);

    private sealed record ValidationCheckSkillSnapshot(Guid Id, SkillSnapshot? Skill);

    private sealed record SkillSnapshot(Guid Id, string SkillId);
}
