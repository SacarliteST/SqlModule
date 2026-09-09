using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class ModuleSessionCleanupProcessor(
    AppDbContext db,
    IOptions<ModuleIntegrationOptions> options,
    TimeProvider timeProvider)
{
    internal async Task<int> CleanupAsync(CancellationToken ct)
    {
        var lifecycle = options.Value.Lifecycle;
        var now = timeProvider.GetUtcNow();
        var retentionBoundary = now.AddDays(-lifecycle.RetentionDays);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);

        var expiredSessions = await db.ModuleSessions
            .Where(session =>
                session.Status == ModuleSessionStatus.Active &&
                session.ExpiresAt != null &&
                session.ExpiresAt < now)
            .OrderBy(session => session.ExpiresAt)
            .Take(lifecycle.BatchSize)
            .ToListAsync(ct);
        foreach (var session in expiredSessions)
        {
            session.MarkExpired(now);
        }

        if (expiredSessions.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        var sessionIds = await db.ModuleSessions
            .Where(session =>
                (session.Status == ModuleSessionStatus.Completed ||
                 session.Status == ModuleSessionStatus.CompletionFailed ||
                 session.Status == ModuleSessionStatus.Expired) &&
                session.UpdatedAt < retentionBoundary)
            .OrderBy(session => session.ExpiresAt ?? session.UpdatedAt)
            .Select(session => session.Id)
            .Take(lifecycle.BatchSize)
            .ToListAsync(ct);

        if (sessionIds.Count == 0)
        {
            await transaction.CommitAsync(ct);
            RecordExpirationMetrics(expiredSessions.Count);
            return 0;
        }

        await db.Attempts
            .Where(attempt => attempt.ModuleSessionId != null &&
                              sessionIds.Contains(attempt.ModuleSessionId.Value))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(attempt => attempt.ModuleSessionId, (Guid?)null),
                ct);

        var deleted = await db.ModuleSessions
            .Where(session => sessionIds.Contains(session.Id))
            .ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
        RecordExpirationMetrics(expiredSessions.Count);
        return deleted;
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
