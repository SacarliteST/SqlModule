namespace SQLModule.Sandbox.Pooling;

internal enum SandboxLeaseDisposition
{
    Recycle,
    Discard,
}

/// <summary>Эксклюзивное право работы с одним поколением sandbox-воркера.</summary>
internal sealed class SandboxLease : IAsyncDisposable
{
    private readonly Func<SandboxLease, ValueTask> release;
    private int disposed;
    private int discardRequested;

    internal SandboxLease(
        SandboxWorker worker,
        long generation,
        Func<SandboxLease, ValueTask> release,
        Guid? leaseId = null)
    {
        Worker = worker;
        this.release = release;
        Generation = generation;
        LeaseId = leaseId ?? Guid.NewGuid();
    }

    internal Guid LeaseId { get; }
    internal SandboxWorker Worker { get; }
    internal long Generation { get; }
    internal SandboxLeaseDisposition Disposition => Volatile.Read(ref discardRequested) == 0
        ? SandboxLeaseDisposition.Recycle
        : SandboxLeaseDisposition.Discard;

    internal void Discard() => Interlocked.Exchange(ref discardRequested, 1);

    public ValueTask DisposeAsync()
    {
        return Interlocked.Exchange(ref disposed, 1) == 0
            ? release(this)
            : ValueTask.CompletedTask;
    }

    public override string ToString() =>
        $"lease={LeaseId:N}; worker={Worker.WorkerId:N}; profile={Worker.Profile.Key}; " +
        $"generation={Generation}; disposition={Disposition}";
}
