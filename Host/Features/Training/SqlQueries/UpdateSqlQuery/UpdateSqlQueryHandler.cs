using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal record UpdateSqlQueryCommand(Guid Id, string QueryText, bool StrictColumnOrder, bool StrictRowOrder)
    : IRequest<Result>;

internal sealed class UpdateSqlQueryHandler(AppDbContext db)
    : IRequestHandler<UpdateSqlQueryCommand, Result>
{
    public async Task<Result> Handle(UpdateSqlQueryCommand command, CancellationToken ct)
    {
        var entity = await db.SqlQueries.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(Error.NotFound("SqlQuery", command.Id));
        }

        entity.Update(command.QueryText, command.StrictColumnOrder, command.StrictRowOrder);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
