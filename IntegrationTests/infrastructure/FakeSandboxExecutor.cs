using SQLModule.Common.Results;
using SQLModule.Sandbox;

namespace SQLModule.IntegrationTests.infrastructure;

internal sealed class FakeSandboxExecutor : ISandboxExecutor
{
    public Result<QueryResultSet>? OverrideRun { get; set; }
    public Result? OverrideSetup { get; set; }

    public Task<Result<QueryResultSet>> RunAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, SandboxQuery query, CancellationToken ct) =>
        Task.FromResult(OverrideRun ?? Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, [], [], 0, 0)));

    public Task<Result> ValidateSetupAsync(
        SandboxDbmsSpec dbms, SandboxSetup setup, CancellationToken ct) =>
        Task.FromResult(OverrideSetup ?? Result.Success());
}
