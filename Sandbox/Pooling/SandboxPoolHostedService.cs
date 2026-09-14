using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQLModule.Sandbox.Pooling;

internal sealed class SandboxPoolHostedService(
    ISandboxPoolLifecycle lifecycle,
    IServiceScopeFactory scopeFactory,
    IOptions<SandboxOptions> options,
    SandboxPoolHealthMonitor healthMonitor,
    ILogger<SandboxPoolHostedService> logger) : BackgroundService
{
    private SandboxPoolOptions? activeOptions;
    private int executionStarted;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var poolOptions = options.Value.Pool;
        activeOptions = poolOptions;
        Interlocked.Exchange(ref executionStarted, 1);
        if (!poolOptions.Enabled)
        {
            return;
        }

        logger.LogInformation("Запущено обслуживание локального пула тёплых sandbox-контейнеров");
        var consecutiveFailures = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var successful = await RunMaintenanceSafelyAsync(stoppingToken);
            consecutiveFailures = successful ? 0 : consecutiveFailures + 1;
            var delay = successful
                ? TimeSpan.FromSeconds(poolOptions.HealthCheckIntervalSeconds)
                : CalculateRestartDelay(consecutiveFailures, poolOptions.RestartBackoffMaxSeconds);

            if (!successful)
            {
                logger.LogWarning(
                    "Обслуживание sandbox-пула завершилось неуспешно; повтор через {DelayMs} мс, серия сбоев {FailureCount}",
                    delay.TotalMilliseconds,
                    consecutiveFailures);
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref executionStarted) == 0)
        {
            return;
        }

        var poolOptions = activeOptions!;
        try
        {
            await base.StopAsync(cancellationToken);
        }
        finally
        {
            if (poolOptions.Enabled)
            {
                using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                shutdown.CancelAfter(TimeSpan.FromSeconds(poolOptions.ShutdownTimeoutSeconds));
                await lifecycle.DrainAsync(shutdown.Token);
            }
        }
    }

    internal static TimeSpan CalculateRestartDelay(int consecutiveFailures, int maximumSeconds)
    {
        var exponent = Math.Min(Math.Max(consecutiveFailures - 1, 0), 30);
        var baseMilliseconds = Math.Min(maximumSeconds * 1000L, 1000L << exponent);
        var jitterMilliseconds = Random.Shared.NextInt64(0, Math.Max(baseMilliseconds / 5, 1));
        return TimeSpan.FromMilliseconds(Math.Min(maximumSeconds * 1000L, baseMilliseconds + jitterMilliseconds));
    }

    private async Task<bool> RunMaintenanceSafelyAsync(CancellationToken cancellationToken)
    {
        var poolOptions = activeOptions!;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var source = scope.ServiceProvider.GetRequiredService<ISandboxPoolProfileSource>();
            var dbmsProfiles = await source.GetProfilesAsync(cancellationToken);
            var profiles = dbmsProfiles
                .Select(spec => (Spec: spec, Limits: SandboxPoolProfileResolver.Find(poolOptions, spec.SystemName)))
                .Where(value => value.Limits is not null)
                .Select(value => new SandboxWorkerProfile(value.Spec, value.Limits!))
                .ToArray();
            var missingRequiredProfiles = poolOptions.Profiles
                .Where(pair => pair.Value.MinSize > 0)
                .Select(pair => pair.Key)
                .Where(profileName => !dbmsProfiles.Any(spec =>
                    SandboxPoolProfileResolver.Matches(profileName, spec.SystemName)))
                .ToArray();
            if (missingRequiredProfiles.Length > 0)
            {
                foreach (var profile in missingRequiredProfiles)
                {
                    healthMonitor.ReportUnavailable(profile);
                }

                logger.LogWarning(
                    "Не найдены разрешённые конфигурации СУБД для обязательных sandbox-профилей {Profiles}",
                    String.Join(',', missingRequiredProfiles));
            }

            var maintained = await lifecycle.MaintainAsync(profiles, cancellationToken);
            return missingRequiredProfiles.Length == 0 && maintained;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Не удалось выполнить цикл обслуживания sandbox-пула; тип сбоя {FailureType}",
                exception.GetType().Name);
            return false;
        }
    }
}
