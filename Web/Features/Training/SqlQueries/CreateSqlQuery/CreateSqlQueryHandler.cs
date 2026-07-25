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
    ISqlQueryValidationRunner validationRunner,
    AppDbContext db)
    : IRequestHandler<CreateSqlQueryCommand, Result<SqlQueryResponse>>
{
    public async Task<Result<SqlQueryResponse>> Handle(CreateSqlQueryCommand command, CancellationToken ct)
    {
        var run = await validationRunner.ValidateAsync(command.TargetDbId, command.QueryText, ct);
        if (!run.IsSuccess)
        {
            return Result<SqlQueryResponse>.Fail(run.Error!);
        }

        var entity = Domain.Training.SqlQuery.Create(
            command.QueryText, command.StrictColumnOrder, command.StrictRowOrder, command.TargetDbId);
        entity.SetExpectedResult(GoldenResult.Serialize(run.Value!));

        db.SqlQueries.Add(entity);
        await db.SaveChangesAsync(ct);
        return SqlQueryMappings.ToResponse(entity);
    }
}
