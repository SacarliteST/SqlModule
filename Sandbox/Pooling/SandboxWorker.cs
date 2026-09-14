namespace SQLModule.Sandbox.Pooling;

/// <summary>Дескриптор контейнера и его текущего состояния внутри локального пула.</summary>
internal sealed class SandboxWorker
{
    private readonly object sync = new();
    private long generation;
    private SandboxWorkerState state = SandboxWorkerState.Starting;

    internal SandboxWorker(
        SandboxWorkerProfile profile,
        string containerId,
        string host,
        int mappedPort,
        Guid? workerId = null)
    {
        Profile = profile;
        ContainerId = containerId;
        Host = host;
        MappedPort = mappedPort;
        WorkerId = workerId ?? Guid.NewGuid();
    }

    internal Guid WorkerId { get; }
    internal SandboxWorkerProfile Profile { get; }
    internal string ContainerId { get; }
    internal string Host { get; }
    internal int MappedPort { get; }

    internal long Generation
    {
        get
        {
            lock (sync)
            {
                return generation;
            }
        }
    }

    internal SandboxWorkerState State
    {
        get
        {
            lock (sync)
            {
                return state;
            }
        }
    }

    internal bool TryMarkReady()
    {
        lock (sync)
        {
            if (state != SandboxWorkerState.Starting)
            {
                return false;
            }

            state = SandboxWorkerState.Ready;
            return true;
        }
    }

    internal bool TryLease(out long leaseGeneration)
    {
        lock (sync)
        {
            if (state != SandboxWorkerState.Ready)
            {
                leaseGeneration = generation;
                return false;
            }

            generation++;
            leaseGeneration = generation;
            state = SandboxWorkerState.Leased;
            return true;
        }
    }

    internal bool TryBeginRecycling(long leaseGeneration) =>
        TryTransitionForGeneration(leaseGeneration, SandboxWorkerState.Leased, SandboxWorkerState.Recycling);

    internal bool TryCompleteRecycling(long leaseGeneration) =>
        TryTransitionForGeneration(leaseGeneration, SandboxWorkerState.Recycling, SandboxWorkerState.Ready);

    internal bool TryMarkUnhealthy()
    {
        lock (sync)
        {
            if (state is SandboxWorkerState.Unhealthy or SandboxWorkerState.Disposed)
            {
                return false;
            }

            state = SandboxWorkerState.Unhealthy;
            return true;
        }
    }

    internal bool TryMarkUnhealthy(long leaseGeneration)
    {
        lock (sync)
        {
            if (generation != leaseGeneration || state != SandboxWorkerState.Leased)
            {
                return false;
            }

            state = SandboxWorkerState.Unhealthy;
            return true;
        }
    }

    internal bool TryMarkDisposed()
    {
        lock (sync)
        {
            if (state == SandboxWorkerState.Disposed)
            {
                return false;
            }

            state = SandboxWorkerState.Disposed;
            return true;
        }
    }

    public override string ToString() =>
        $"worker={WorkerId:N}; profile={Profile.Key}; generation={Generation}; state={State}";

    private bool TryTransitionForGeneration(
        long leaseGeneration,
        SandboxWorkerState expected,
        SandboxWorkerState next)
    {
        lock (sync)
        {
            if (generation != leaseGeneration || state != expected)
            {
                return false;
            }

            state = next;
            return true;
        }
    }
}
