using SQLModule.Common.Results;

namespace SQLModule.Sandbox.Pooling;

internal interface ISandboxWorkerFactory
{
    ValueTask<Result<SandboxWorker>> CreateAsync(
        SandboxWorkerProfile profile,
        CancellationToken cancellationToken);

    ValueTask<Result> DeleteAsync(
        SandboxWorker worker,
        CancellationToken cancellationToken);

    ValueTask<bool> IsHealthyAsync(
        SandboxWorker worker,
        CancellationToken cancellationToken);
}
