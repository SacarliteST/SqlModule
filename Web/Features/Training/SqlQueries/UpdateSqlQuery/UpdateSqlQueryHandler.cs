using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal record UpdateSqlQueryCommand(Guid Id, string QueryText, bool StrictColumnOrder, bool StrictRowOrder)
    : IRequest<Result>;

internal sealed class UpdateSqlQueryHandler(
    ISqlQueryValidationRunner validationRunner,
    AppDbContext db)
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

        entity.Update(command.QueryText, command.StrictColumnOrder, command.StrictRowOrder);
        entity.SetExpectedResult(GoldenResult.Serialize(run.Value!));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
