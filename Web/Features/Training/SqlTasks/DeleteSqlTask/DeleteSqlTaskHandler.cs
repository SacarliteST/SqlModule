using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal record DeleteSqlTaskCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteSqlTaskHandler(AppDbContext db)
    : IRequestHandler<DeleteSqlTaskCommand, Result>
{
    public async Task<Result> Handle(DeleteSqlTaskCommand command, CancellationToken ct)
    {
        var entity = await db.SqlTasks.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(SqlTaskErrors.NotFound(command.Id));
        }

        if (await db.Attempts.AnyAsync(a => a.TaskId == command.Id, ct))
        {
            return Result.Fail(SqlTaskErrors.HasAttempts(command.Id));
        }

        db.SqlTasks.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
