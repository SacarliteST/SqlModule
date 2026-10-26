using System.Threading.RateLimiting;

namespace SQLModule.Application.Training.Bank;

/// <summary>Удерживает разрешение на одну выполняющуюся тренировочную попытку.</summary>
public sealed class TrainingAttemptLease : IDisposable
{
    private readonly RateLimitLease concurrencyLease;
    private readonly RateLimitLease intervalLease;

    /// <summary>Создаёт составной lease двух встроенных ограничителей.</summary>
    public TrainingAttemptLease(RateLimitLease concurrencyLease, RateLimitLease intervalLease)
    {
        this.concurrencyLease = concurrencyLease;
        this.intervalLease = intervalLease;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        intervalLease.Dispose();
        concurrencyLease.Dispose();
    }
}
