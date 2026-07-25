using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal interface ISqlQueryValidationRunner
{
    Task<Result<QueryResultSet>> ValidateAsync(
        Guid targetDbId,
        string queryText,
        CancellationToken ct);
}

internal sealed class SqlQueryValidationRunner(
    ITaskMaterializer materializer,
    ISandboxExecutor executor,
    IOptions<SandboxOptions> sandboxOptions)
    : ISqlQueryValidationRunner
{
    public async Task<Result<QueryResultSet>> ValidateAsync(
        Guid targetDbId,
        string queryText,
        CancellationToken ct)
    {
        var materialized = await materializer.MaterializeAsync(targetDbId, ct);
        if (!materialized.IsSuccess)
        {
            return Result<QueryResultSet>.Fail(materialized.Error!);
        }

        var options = sandboxOptions.Value;
        var run = await executor.RunAsync(
            materialized.Value!.Dbms.ToSandboxSpec(),
            materialized.Value.Setup,
            new SandboxQuery(queryText, options.DefaultQueryTimeoutSeconds, options.MaxRows),
            ct);

        if (!run.IsSuccess)
        {
            return Result<QueryResultSet>.Fail(run.Error!);
        }

        if (!run.Value!.Succeeded)
        {
            return Result<QueryResultSet>.Fail(
                SqlQueryErrors.ReferenceInvalid(run.Value.Error ?? string.Empty));
        }

        return run;
    }
}
