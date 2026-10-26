using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace SQLModule.Application.Training.Bank;

/// <summary>Реализует локальные лимиты тренировки встроенными .NET rate limiters.</summary>
public sealed class TrainingAttemptGate : ITrainingAttemptGate, IDisposable
{
    private readonly PartitionedRateLimiter<Guid> concurrencyLimiter;
    private readonly PartitionedRateLimiter<Guid> intervalLimiter;

    /// <inheritdoc />
    public int RetryAfterSeconds { get; }

    /// <summary>Создаёт ограничители из конфигурации банка.</summary>
    public TrainingAttemptGate(IOptions<TaskBankOptions> options)
    {
        var interval = TimeSpan.FromSeconds(options.Value.SubmissionIntervalSeconds);
        RetryAfterSeconds = options.Value.RetryAfterSeconds;
        concurrencyLimiter = PartitionedRateLimiter.Create<Guid, Guid>(userId =>
            RateLimitPartition.GetConcurrencyLimiter(userId, _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
        intervalLimiter = PartitionedRateLimiter.Create<Guid, Guid>(userId =>
            RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1,
                Window = interval,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
    }

    /// <inheritdoc />
    public async ValueTask<TrainingAttemptLease?> TryAcquireAsync(Guid userId, CancellationToken ct)
    {
        var concurrencyLease = await concurrencyLimiter.AcquireAsync(userId, 1, ct);
        if (!concurrencyLease.IsAcquired)
        {
            concurrencyLease.Dispose();
            return null;
        }

        var intervalLease = await intervalLimiter.AcquireAsync(userId, 1, ct);
        if (!intervalLease.IsAcquired)
        {
            intervalLease.Dispose();
            concurrencyLease.Dispose();
            return null;
        }

        return new TrainingAttemptLease(concurrencyLease, intervalLease);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        intervalLimiter.Dispose();
        concurrencyLimiter.Dispose();
    }
}
