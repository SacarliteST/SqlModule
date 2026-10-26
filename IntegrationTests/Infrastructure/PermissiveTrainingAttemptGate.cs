using System.Threading.RateLimiting;
using SQLModule.Application.Training.Bank;

namespace SQLModule.IntegrationTests.Infrastructure;

/// <summary>Не ограничивает частоту отправок в тестах, не посвящённых rate limiting.</summary>
internal sealed class PermissiveTrainingAttemptGate : ITrainingAttemptGate, IDisposable
{
    private readonly ConcurrencyLimiter concurrencyLimiter = new(new ConcurrencyLimiterOptions
    {
        PermitLimit = 10_000,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

    private readonly FixedWindowRateLimiter intervalLimiter = new(new FixedWindowRateLimiterOptions
    {
        PermitLimit = 10_000,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    });

    /// <inheritdoc />
    public int RetryAfterSeconds => 1;

    /// <inheritdoc />
    public async ValueTask<TrainingAttemptLease?> TryAcquireAsync(Guid userId, CancellationToken ct)
    {
        var concurrencyLease = await concurrencyLimiter.AcquireAsync(1, ct);
        var intervalLease = await intervalLimiter.AcquireAsync(1, ct);
        return new TrainingAttemptLease(concurrencyLease, intervalLease);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        concurrencyLimiter.Dispose();
        intervalLimiter.Dispose();
    }
}
