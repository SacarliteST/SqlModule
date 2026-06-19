using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal record DeleteSqlQueryCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteSqlQueryHandler(AppDbContext db)
    : IRequestHandler<DeleteSqlQueryCommand, Result>
{
    public async Task<Result> Handle(DeleteSqlQueryCommand command, CancellationToken ct)
    {
        var entity = await db.SqlQueries.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(Error.NotFound("SqlQuery", command.Id));
        }

        var inUse = await db.SqlTasks.AnyAsync(t => t.SqlQueryId == command.Id, ct)
                    || await db.Attempts.AnyAsync(a => a.QueryId == command.Id, ct);

        if (inUse)
        {
            return Result.Fail(Error.Conflict(
                "SqlQuery.InUse",
                "Запрос используется заданиями или попытками."));
        }

        db.SqlQueries.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
