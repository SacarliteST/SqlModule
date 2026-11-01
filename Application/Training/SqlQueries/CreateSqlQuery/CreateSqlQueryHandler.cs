using Microsoft.Extensions.Options;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.SqlQueries;

public record CreateSqlQueryCommand(
    Guid TargetDbId,
    string QueryText,
    bool StrictColumnOrder,
    bool StrictRowOrder)
    : IRequest<Result<SqlQueryResponse>>;

public sealed class CreateSqlQueryHandler(
    ISqlQueryValidationRunner validationRunner,
    IApplicationDbContext db)
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
