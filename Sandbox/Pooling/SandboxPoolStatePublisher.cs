namespace SQLModule.Sandbox.Pooling;

/// <summary>Публикует состояние пула в метрики и health-монитор.</summary>
internal sealed class SandboxPoolStatePublisher(SandboxPoolHealthMonitor healthMonitor)
{
    internal void Publish(ProfilePool pool, string systemName)
    {
        var workers = pool.SnapshotWorkers();
        SandboxPoolTelemetry.RecordWorkerStates(
            systemName,
            workers,
            Math.Max(pool.Allocated - workers.Count, 0));
        healthMonitor.Report(
            systemName,
            workers.Count(worker => worker.State is
                SandboxWorkerState.Ready or
                SandboxWorkerState.Leased or
                SandboxWorkerState.Recycling));
    }
}
