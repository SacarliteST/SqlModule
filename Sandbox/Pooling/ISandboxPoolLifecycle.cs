namespace SQLModule.Sandbox.Pooling;

internal interface ISandboxPoolLifecycle
{
    ValueTask<bool> MaintainAsync(
        IReadOnlyCollection<SandboxWorkerProfile> profiles,
        CancellationToken cancellationToken);

    ValueTask DrainAsync(CancellationToken cancellationToken);
}
