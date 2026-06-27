using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Data.Core;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal record CreateSqlQueryCommand(
    Guid TargetDbId,
    string QueryText,
    bool StrictColumnOrder,
    bool StrictRowOrder)
    : IRequest<Result<SqlQueryResponse>>;

internal sealed class CreateSqlQueryHandler(
    ITaskMaterializer materializer,
    ISandboxExecutor executor,
    IOptions<SandboxOptions> sandboxOptions,
    AppDbContext db)
    : IRequestHandler<CreateSqlQueryCommand, Result<SqlQueryResponse>>
{
    public async Task<Result<SqlQueryResponse>> Handle(CreateSqlQueryCommand command, CancellationToken ct)
    {
        var mat = await materializer.MaterializeAsync(command.TargetDbId, ct);
        if (!mat.IsSuccess)
        {
            return Result<SqlQueryResponse>.Fail(mat.Error!);
        }

        var opts = sandboxOptions.Value;
        var run = await executor.RunAsync(
            mat.Value!.Dbms.ToSandboxSpec(),
            mat.Value.Setup,
            new SandboxQuery(command.QueryText, opts.DefaultQueryTimeoutSeconds, opts.MaxRows),
            ct);

        if (!run.IsSuccess)
        {
            return Result<SqlQueryResponse>.Fail(run.Error!);
        }

        if (!run.Value!.Succeeded)
        {
            return Result<SqlQueryResponse>.Fail(SqlQueryErrors.ReferenceInvalid(run.Value.Error ?? String.Empty));
        }

        var entity = Domain.Training.SqlQuery.Create(
            command.QueryText, command.StrictColumnOrder, command.StrictRowOrder, command.TargetDbId);
        entity.SetExpectedResult(GoldenResult.Serialize(run.Value));

        db.SqlQueries.Add(entity);
        await db.SaveChangesAsync(ct);
        return SqlQueryMappings.ToResponse(entity);
    }
}
