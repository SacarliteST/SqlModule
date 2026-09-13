using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;
using SQLModule.Web.Common;

namespace SQLModule.UnitTests.Sandbox;

public sealed class SandboxPoolObservabilityTests
{
    [Fact(DisplayName = "Pool health: обязательный профиль становится unhealthy только после grace period")]
    public async Task Health_UsesStartupGraceAndRecoversWhenWorkerIsAvailable()
    {
        var clock = new TestTimeProvider();
        var options = CreateEnabledOptions();
        var monitor = new SandboxPoolHealthMonitor(options, clock);
        var healthCheck = new SandboxPoolHealthCheck(monitor);

        monitor.GetReport().IsHealthy.ShouldBeTrue();
        clock.Advance(TimeSpan.FromSeconds(61));

        var unavailable = monitor.GetReport();
        unavailable.IsHealthy.ShouldBeFalse();
        unavailable.UnavailableProfiles.ShouldBe(["postgres"]);
        (await healthCheck.CheckHealthAsync(new HealthCheckContext())).Status
            .ShouldBe(HealthStatus.Unhealthy);

        monitor.Report("postgres", 1);
        monitor.GetReport().IsHealthy.ShouldBeTrue();

        monitor.Report("postgres", 0);
        clock.Advance(TimeSpan.FromSeconds(30));
        monitor.GetReport().IsHealthy.ShouldBeTrue();
        clock.Advance(TimeSpan.FromSeconds(31));
        monitor.GetReport().IsHealthy.ShouldBeFalse();
    }

    [Fact(DisplayName = "Pool health: выключенный пул всегда healthy")]
    public void Health_DisabledPoolIsHealthy()
    {
        var monitor = new SandboxPoolHealthMonitor(Options.Create(new SandboxOptions()));

        var report = monitor.GetReport();

        report.IsHealthy.ShouldBeTrue();
        report.IsEnabled.ShouldBeFalse();
        report.UnavailableProfiles.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Pool metrics: публикуются состояния и этапы без идентификаторов и секретов")]
    public void Metrics_ExposeOnlyLowCardinalityTags()
    {
        const string profile = "observability-test";
        var measurements = new List<MeasurementRecord>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == SandboxPoolTelemetry.InstrumentationName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Add(new MeasurementRecord(instrument.Name, value, ToTags(tags))));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            measurements.Add(new MeasurementRecord(instrument.Name, value, ToTags(tags))));
        listener.Start();

        SandboxPoolTelemetry.RecordWorkerStates(profile, [], 1);
        SandboxPoolTelemetry.RecordContainerCreated(profile);
        SandboxPoolTelemetry.RecordAcquireTimeout(profile);
        SandboxPoolTelemetry.RecordCleanupFailure(profile);
        SandboxPoolTelemetry.RecordReplacement(profile, "health");
        SandboxPoolTelemetry.OperationStarted(profile);
        SandboxPoolTelemetry.OperationFinished(profile);
        SandboxPoolTelemetry.RecordLeaseWait(profile, TimeSpan.FromMilliseconds(2), "success");
        SandboxPoolTelemetry.RecordPreparation(profile, TimeSpan.FromMilliseconds(3), "success");
        SandboxPoolTelemetry.RecordExecution(profile, TimeSpan.FromMilliseconds(4), "failure");
        SandboxPoolTelemetry.RecordCleanup(profile, TimeSpan.FromMilliseconds(5), "success");
        SandboxPoolTelemetry.RecordCycle(profile, TimeSpan.FromMilliseconds(6), "failure");
        listener.RecordObservableInstruments();

        measurements.ShouldContain(value =>
            value.Instrument == "sqlmodule_sandbox_pool_workers" &&
            value.Tags["profile"] == profile &&
            value.Tags["state"] == "starting" &&
            value.Value == 1);
        measurements.ShouldContain(value => value.Instrument == "sqlmodule_sandbox_pool_lease_wait_ms");
        measurements.ShouldContain(value => value.Instrument == "sqlmodule_sandbox_pool_preparation_ms");
        measurements.ShouldContain(value => value.Instrument == "sqlmodule_sandbox_pool_execution_ms");
        measurements.ShouldContain(value => value.Instrument == "sqlmodule_sandbox_pool_cleanup_ms");
        measurements.ShouldContain(value => value.Instrument == "sqlmodule_sandbox_pool_cycle_ms");

        var allowedTags = new HashSet<string>(StringComparer.Ordinal) { "profile", "state", "outcome", "reason" };
        measurements.SelectMany(value => value.Tags.Keys).ShouldAllBe(tag => allowedTags.Contains(tag));
    }

    private static IOptions<SandboxOptions> CreateEnabledOptions()
    {
        var options = new SandboxOptions
        {
            Pool = new SandboxPoolOptions
            {
                Enabled = true,
                StartupGracePeriodSeconds = 60,
            },
        };
        options.Pool.Profiles.Add("postgres", new SandboxPoolProfileOptions { MinSize = 1, MaxSize = 1 });
        options.Pool.Profiles.Add("mysql", new SandboxPoolProfileOptions { MinSize = 0, MaxSize = 1 });
        return Options.Create(options);
    }

    private static Dictionary<string, string?> ToTags(ReadOnlySpan<KeyValuePair<string, object?>> tags) =>
        tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value?.ToString());

    private sealed record MeasurementRecord(
        string Instrument,
        double Value,
        IReadOnlyDictionary<string, string?> Tags);

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => utcNow;

        internal void Advance(TimeSpan duration) => utcNow += duration;
    }
}
