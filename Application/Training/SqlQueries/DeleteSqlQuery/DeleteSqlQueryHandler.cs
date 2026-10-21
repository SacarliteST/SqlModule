using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.SqlQueries;

public record DeleteSqlQueryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteSqlQueryHandler(IApplicationDbContext db)
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
