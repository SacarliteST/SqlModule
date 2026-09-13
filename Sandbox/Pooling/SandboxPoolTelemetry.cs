using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace SQLModule.Sandbox.Pooling;

internal static class SandboxPoolTelemetry
{
    internal const string InstrumentationName = "SQLModule.SandboxPool";

    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly ConcurrentDictionary<(string Profile, string State), long> WorkerStates = new();
    private static readonly ObservableGauge<long> WorkersGauge = Meter.CreateObservableGauge(
        "sqlmodule_sandbox_pool_workers",
        ObserveWorkers);
    private static readonly Counter<long> ContainersCreated = Meter.CreateCounter<long>(
        "sqlmodule_sandbox_pool_containers_created_total");
    private static readonly Counter<long> ContainersDeleted = Meter.CreateCounter<long>(
        "sqlmodule_sandbox_pool_containers_deleted_total");
    private static readonly Counter<long> AcquireTimeouts = Meter.CreateCounter<long>(
        "sqlmodule_sandbox_pool_acquire_timeouts_total");
    private static readonly Counter<long> CleanupFailures = Meter.CreateCounter<long>(
        "sqlmodule_sandbox_pool_cleanup_failures_total");
    private static readonly Counter<long> Replacements = Meter.CreateCounter<long>(
        "sqlmodule_sandbox_pool_replacements_total");
    private static readonly UpDownCounter<long> ActiveOperations = Meter.CreateUpDownCounter<long>(
        "sqlmodule_sandbox_pool_active_operations");
    private static readonly Counter<long> ForcedShutdowns = Meter.CreateCounter<long>(
        "sqlmodule_sandbox_pool_forced_shutdown_total");
    private static readonly Histogram<double> LeaseWait = Meter.CreateHistogram<double>(
        "sqlmodule_sandbox_pool_lease_wait_ms",
        "ms");
    private static readonly Histogram<double> Preparation = Meter.CreateHistogram<double>(
        "sqlmodule_sandbox_pool_preparation_ms",
        "ms");
    private static readonly Histogram<double> Execution = Meter.CreateHistogram<double>(
        "sqlmodule_sandbox_pool_execution_ms",
        "ms");
    private static readonly Histogram<double> Cleanup = Meter.CreateHistogram<double>(
        "sqlmodule_sandbox_pool_cleanup_ms",
        "ms");
    private static readonly Histogram<double> FullCycle = Meter.CreateHistogram<double>(
        "sqlmodule_sandbox_pool_cycle_ms",
        "ms");

    internal static void RecordWorkerStates(string profile, IReadOnlyCollection<SandboxWorker> workers, int starting)
    {
        RecordState(profile, "starting", starting);
        RecordState(profile, "ready", workers.Count(worker => worker.State == SandboxWorkerState.Ready));
        RecordState(profile, "leased", workers.Count(worker => worker.State == SandboxWorkerState.Leased));
        RecordState(profile, "recycling", workers.Count(worker => worker.State == SandboxWorkerState.Recycling));
        RecordState(profile, "unhealthy", workers.Count(worker => worker.State == SandboxWorkerState.Unhealthy));
    }

    internal static void RecordContainerCreated(string profile) => ContainersCreated.Add(1, ProfileTag(profile));
    internal static void RecordContainerDeleted(string profile) => ContainersDeleted.Add(1, ProfileTag(profile));
    internal static void RecordAcquireTimeout(string profile) => AcquireTimeouts.Add(1, ProfileTag(profile));
    internal static void RecordCleanupFailure(string profile) => CleanupFailures.Add(1, ProfileTag(profile));
    internal static void RecordReplacement(string profile, string reason) => Replacements.Add(
        1,
        ProfileTag(profile),
        new KeyValuePair<string, object?>("reason", reason));
    internal static void RecordForcedShutdown(long activeLeases) => ForcedShutdowns.Add(activeLeases);
    internal static void OperationStarted(string profile) => ActiveOperations.Add(1, ProfileTag(profile));
    internal static void OperationFinished(string profile) => ActiveOperations.Add(-1, ProfileTag(profile));
    internal static void RecordLeaseWait(string profile, TimeSpan duration, string outcome) => LeaseWait.Record(
        duration.TotalMilliseconds,
        ProfileTag(profile),
        new KeyValuePair<string, object?>("outcome", outcome));
    internal static void RecordPreparation(string profile, TimeSpan duration, string outcome) => Preparation.Record(
        duration.TotalMilliseconds,
        ProfileTag(profile),
        new KeyValuePair<string, object?>("outcome", outcome));
    internal static void RecordExecution(string profile, TimeSpan duration, string outcome) => Execution.Record(
        duration.TotalMilliseconds,
        ProfileTag(profile),
        new KeyValuePair<string, object?>("outcome", outcome));
    internal static void RecordCleanup(string profile, TimeSpan duration, string outcome) => Cleanup.Record(
        duration.TotalMilliseconds,
        ProfileTag(profile),
        new KeyValuePair<string, object?>("outcome", outcome));
    internal static void RecordCycle(string profile, TimeSpan duration, string outcome) => FullCycle.Record(
        duration.TotalMilliseconds,
        ProfileTag(profile),
        new KeyValuePair<string, object?>("outcome", outcome));

    private static void RecordState(string profile, string state, long value) =>
        WorkerStates[(profile, state)] = value;

    private static IEnumerable<Measurement<long>> ObserveWorkers() => WorkerStates.Select(pair =>
        new Measurement<long>(
            pair.Value,
            new KeyValuePair<string, object?>("profile", pair.Key.Profile),
            new KeyValuePair<string, object?>("state", pair.Key.State)));

    private static KeyValuePair<string, object?> ProfileTag(string profile) => new("profile", profile);
}
