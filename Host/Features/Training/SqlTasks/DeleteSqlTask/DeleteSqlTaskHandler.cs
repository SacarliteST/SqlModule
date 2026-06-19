using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal record DeleteSqlTaskCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteSqlTaskHandler(AppDbContext db)
    : IRequestHandler<DeleteSqlTaskCommand, Result>
{
    public async Task<Result> Handle(DeleteSqlTaskCommand command, CancellationToken ct)
    {
        var entity = await db.SqlTasks.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(Error.NotFound("SqlTask", command.Id));
        }

        if (await db.Attempts.AnyAsync(a => a.TaskId == command.Id, ct))
        {
            return Result.Fail(Error.Conflict(
                "SqlTask.HasAttempts",
                $"Задание '{command.Id}' имеет попытки выполнения и не может быть удалено."));
        }

        db.SqlTasks.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
