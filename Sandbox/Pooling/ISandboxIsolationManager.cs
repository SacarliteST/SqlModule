using System.Data.Common;
using SQLModule.Common.Results;

namespace SQLModule.Sandbox.Pooling;

internal interface ISandboxIsolationManager
{
    Task<Result<SandboxIsolationNamespace>> CreateAsync(SandboxLease lease, CancellationToken ct);
    Task<Result<DbConnection>> OpenSetupConnectionAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
    Task<Result> GrantRunnerAccessAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
    Task<Result<DbConnection>> OpenRunnerConnectionAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
    Task<Result> CleanupAsync(
        SandboxLease lease,
        SandboxIsolationNamespace sandboxNamespace,
        CancellationToken ct);
}
