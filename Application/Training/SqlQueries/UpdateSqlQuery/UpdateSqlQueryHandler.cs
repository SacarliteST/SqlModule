using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Application.TaskMaterialization;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Sandbox;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.SqlQueries;

public record UpdateSqlQueryCommand(Guid Id, string QueryText, bool StrictColumnOrder, bool StrictRowOrder)
    : IRequest<Result>;

public sealed class UpdateSqlQueryHandler(
    ISqlQueryValidationRunner validationRunner,
    IApplicationDbContext db)
    : IRequestHandler<UpdateSqlQueryCommand, Result>
{
    public async Task<Result> Handle(UpdateSqlQueryCommand command, CancellationToken ct)
    {
        var entity = await db.SqlQueries.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(SqlQueryErrors.NotFound(command.Id));
        }

        var run = await validationRunner.ValidateAsync(entity.TargetDbId, command.QueryText, ct);
        if (!run.IsSuccess)
        {
            return Result.Fail(run.Error!);
        }

        entity.Update(entity.TargetDbId, command.QueryText, command.StrictColumnOrder, command.StrictRowOrder);
        entity.SetExpectedResult(GoldenResult.Serialize(run.Value!));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
