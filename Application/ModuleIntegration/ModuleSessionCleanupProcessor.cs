using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Application.Persistence;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Application.ModuleIntegration;

public sealed class ModuleSessionCleanupProcessor(
    IApplicationDbContext db,
    IOptions<ModuleIntegrationOptions> options,
    PracticeCompletionOutbox completionOutbox,
    TimeProvider timeProvider,
    ILogger<ModuleSessionCleanupProcessor> logger)
{
    public async Task<int> CleanupAsync(CancellationToken ct)
    {
        var lifecycle = options.Value.Lifecycle;
        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);

        var expiredSessions = await db.ModuleSessions
            .Where(session =>
                session.Status == ModuleSessionStatus.Active &&
                session.ExpiresAt != null &&
                session.ExpiresAt <= now)
            .OrderBy(session => session.ExpiresAt)
            .Take(lifecycle.BatchSize)
            .ToListAsync(ct);
        foreach (var session in expiredSessions)
        {
            var attemptsUsed = await db.Attempts.CountAsync(value =>
                value.ModuleSessionId == session.Id && value.CountsTowardLimit, ct);
            var bestScore = await db.Attempts
                .Where(value => value.ModuleSessionId == session.Id && value.CountsTowardLimit)
                .MaxAsync(value => (int?)value.Score, ct) ?? 0;
            session.SetFinalResult(bestScore, FinalizationReason.Expired, now);
            await completionOutbox.TryAddAsync(
                session, bestScore, attemptsUsed, FinalizationReason.Expired, now, null, ct);
            session.MarkExpired(now);
            logger.LogInformation(
                "Истёкшая сессия {SessionId} финализирована с баллом {FinalScore}",
                session.Id, bestScore);
        }

        if (expiredSessions.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
        RecordExpirationMetrics(expiredSessions.Count);
        return 0;
    }

    private static void RecordExpirationMetrics(int expiredCount)
    {
        if (expiredCount == 0)
        {
            return;
        }

        ModuleIntegrationTelemetry.RecordSessionTransition(
            ModuleSessionStatus.Active,
            ModuleSessionStatus.Expired,
            expiredCount);
        ModuleIntegrationTelemetry.RecordExpiredSessions(expiredCount);
    }
}
