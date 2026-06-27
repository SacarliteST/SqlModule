using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal record DeleteSqlQueryCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteSqlQueryHandler(AppDbContext db)
    : IRequestHandler<DeleteSqlQueryCommand, Result>
{
    public async Task<Result> Handle(DeleteSqlQueryCommand command, CancellationToken ct)
    {
        var entity = await db.SqlQueries.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(SqlQueryErrors.NotFound(command.Id));
        }

        var inUse = await db.SqlTasks.AnyAsync(t => t.SqlQueryId == command.Id, ct);

        if (inUse)
        {
            return Result.Fail(SqlQueryErrors.InUse);
        }

        db.SqlQueries.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
