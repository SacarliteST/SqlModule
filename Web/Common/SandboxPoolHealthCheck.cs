using Microsoft.Extensions.Diagnostics.HealthChecks;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common;

internal sealed class SandboxPoolHealthCheck(ISandboxPoolHealthMonitor monitor) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var report = monitor.GetReport();
        if (report.IsHealthy)
        {
            var description = report.IsEnabled
                ? "Обязательные профили sandbox-пула доступны."
                : "Sandbox-пул выключен.";
            return Task.FromResult(HealthCheckResult.Healthy(description));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            $"Недоступны обязательные профили sandbox-пула: {String.Join(',', report.UnavailableProfiles)}."));
    }
}
