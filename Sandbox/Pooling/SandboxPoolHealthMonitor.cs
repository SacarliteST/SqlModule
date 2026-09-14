using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace SQLModule.Sandbox.Pooling;

internal sealed class SandboxPoolHealthMonitor(
    IOptions<SandboxOptions> options,
    TimeProvider? timeProvider = null)
    : ISandboxPoolHealthMonitor
{
    private readonly SandboxPoolOptions poolOptions = options.Value.Pool;
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, DateTimeOffset> unavailableSince =
        new(StringComparer.OrdinalIgnoreCase);

    public SandboxPoolHealthReport GetReport()
    {
        if (!poolOptions.Enabled)
        {
            return new SandboxPoolHealthReport(true, false, []);
        }

        var gracePeriod = TimeSpan.FromSeconds(poolOptions.StartupGracePeriodSeconds);
        var unavailable = poolOptions.Profiles
            .Where(pair => pair.Value.MinSize > 0)
            .Select(pair => pair.Key)
            .Where(profile => timeProvider.GetUtcNow() -
                              unavailableSince.GetOrAdd(profile, _ => timeProvider.GetUtcNow()) >= gracePeriod)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new SandboxPoolHealthReport(unavailable.Length == 0, true, unavailable);
    }

    internal void Report(string systemName, int operationalWorkers)
    {
        foreach (var profile in poolOptions.Profiles
                     .Where(pair => pair.Value.MinSize > 0 &&
                                    SandboxPoolProfileResolver.Matches(pair.Key, systemName))
                     .Select(pair => pair.Key))
        {
            if (operationalWorkers > 0)
            {
                unavailableSince.TryRemove(profile, out _);
            }
            else
            {
                unavailableSince.TryAdd(profile, timeProvider.GetUtcNow());
            }
        }
    }

    internal void ReportUnavailable(string profile)
    {
        if (poolOptions.Profiles.TryGetValue(profile, out var limits) && limits.MinSize > 0)
        {
            unavailableSince.TryAdd(profile, timeProvider.GetUtcNow());
        }
    }
}
