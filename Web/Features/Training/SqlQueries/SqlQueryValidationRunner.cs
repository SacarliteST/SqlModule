using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.SqlTasks;

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
        var comparisonRowLimit = Math.Max(1, options.ComparisonMaxRows);
        var run = await executor.RunAsync(
            materialized.Value!.Dbms.ToSandboxSpec(),
            materialized.Value.Setup,
            new SandboxQuery(queryText, options.DefaultQueryTimeoutSeconds, comparisonRowLimit),
            ct);

        if (!run.IsSuccess)
        {
            return Result<QueryResultSet>.Fail(run.Error!);
        }

        if (!run.Value!.Succeeded)
        {
            return Result<QueryResultSet>.Fail(
                SqlQueryErrors.ReferenceInvalid(run.Value.Error ?? String.Empty));
        }

        if (run.Value.IsTruncated)
        {
            return Result<QueryResultSet>.Fail(
                SqlTaskErrors.ReferenceResultExceedsComparisonLimit(comparisonRowLimit));
        }

        return run;
    }
}
