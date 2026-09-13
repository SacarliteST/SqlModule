using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class ModuleSessionCleanupProcessor(
    AppDbContext db,
    IOptions<ModuleIntegrationOptions> options,
    TimeProvider timeProvider,
    ILogger<ModuleSessionCleanupProcessor> logger)
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
            await LogExpiredSessionsWithAttemptsAsync(expiredSessions, ct);
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

    /// <summary>
    /// Помечает попытки без финализации отдельным Warning-логом и метрикой —
    /// иначе результат студента (успешный или нет) тихо теряется при
    /// истечении platform-сессии: раньше это вообще нигде не логировалось.
    /// См. SQLTren/PLATFORM.md, раздел про логирование (L3).
    /// </summary>
    private async Task LogExpiredSessionsWithAttemptsAsync(
        IReadOnlyList<ModuleSession> expiredSessions,
        CancellationToken ct)
    {
        var expiredIds = expiredSessions.Select(session => session.Id).ToList();
        var attemptStats = await db.Attempts
            .Where(attempt => attempt.ModuleSessionId != null && expiredIds.Contains(attempt.ModuleSessionId.Value))
            .GroupBy(attempt => attempt.ModuleSessionId!.Value)
            .Select(group => new
            {
                SessionId = group.Key,
                AttemptCount = group.Count(),
                HasCorrectAttempt = group.Any(attempt => attempt.IsCorrect),
            })
            .ToListAsync(ct);

        var statsBySessionId = attemptStats.ToDictionary(stat => stat.SessionId);
        var sessionsWithAttempts = 0;

        foreach (var session in expiredSessions)
        {
            if (!statsBySessionId.TryGetValue(session.Id, out var stats))
            {
                // Сессию открыли и ни разу не отправили SQL — не результат,
                // а обычный неиспользованный запуск, отдельного внимания не стоит.
                continue;
            }

            sessionsWithAttempts++;
            logger.LogWarning(
                "Platform-сессия {SessionId} (студент {UserId}, задание {TaskRef}) истекла без " +
                "финализации: {AttemptCount} попыток, есть успешная попытка: {HasCorrectAttempt}. " +
                "Итоговая оценка не будет передана в Education, пока не появится авто-финализация " +
                "по expiry (Phase 2b).",
                session.Id, session.UserId, session.TaskRef, stats.AttemptCount, stats.HasCorrectAttempt);
        }

        if (sessionsWithAttempts > 0)
        {
            ModuleIntegrationTelemetry.RecordExpiredSessionsWithAttempts(sessionsWithAttempts);
        }
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
