using SQLModule.Common.Results;

namespace SQLModule.Sandbox.Pooling;

internal interface ISandboxLeaseManager
{
    ValueTask<Result<SandboxLease>> AcquireAsync(
        SandboxWorkerProfile profile,
        CancellationToken cancellationToken);
}
