using Microsoft.EntityFrameworkCore;
using SQLModule.Application.ModuleIntegration;

namespace SQLModule.Data.Core;

/// <summary>
/// Claim outbox через один <c>UPDATE ... RETURNING</c> с <c>FOR UPDATE SKIP LOCKED</c>: параллельные экземпляры
/// получают непересекающиеся наборы сообщений и не блокируют друг друга.
/// </summary>
internal sealed class PostgresPendingPublishClaimer(AppDbContext db) : IPendingPublishClaimer
{
    public async Task<IReadOnlyList<Guid>> ClaimAsync(
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken ct)
    {
        var lockedUntil = now.Add(leaseDuration);
        return await db.Database.SqlQuery<Guid>($"""
                UPDATE "PendingPublishes"
                SET "LockedBy" = {owner}, "LockedUntil" = {lockedUntil}
                WHERE "Id" IN (
                    SELECT "Id" FROM "PendingPublishes"
                    WHERE "SentAt" IS NULL
                      AND "DeadLetterAt" IS NULL
                      AND "NextAttemptAt" <= {now}
                      AND ("LockedUntil" IS NULL OR "LockedUntil" <= {now})
                    ORDER BY "NextAttemptAt", "CreatedAt"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED)
                RETURNING "Id" AS "Value"
                """)
            .ToListAsync(ct);
    }
}
