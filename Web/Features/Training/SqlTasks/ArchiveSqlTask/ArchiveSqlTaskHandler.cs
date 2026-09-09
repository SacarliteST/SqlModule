using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed record ArchiveSqlTaskCommand(Guid TaskId) : IRequest<Result<SqlTaskResponse>>;

internal sealed class ArchiveSqlTaskHandler(AppDbContext db)
    : IRequestHandler<ArchiveSqlTaskCommand, Result<SqlTaskResponse>>
{
    public async Task<Result<SqlTaskResponse>> Handle(ArchiveSqlTaskCommand command, CancellationToken ct)
    {
        var task = await db.SqlTasks.SingleOrDefaultAsync(x => x.Id == command.TaskId, ct);
        if (task is null)
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.NotFound(command.TaskId));
        }

        if (task.PublicationStatus == PublicationStatus.Archived)
        {
            return Result<SqlTaskResponse>.Fail(Error.Conflict("SqlTask.AlreadyArchived", "Задание уже находится в архиве."));
        }

        task.Update(task.TaskName, task.TaskText, task.DifficultyLevel, PublicationStatus.Archived);
        await db.SaveChangesAsync(ct);
        return SqlTaskMappings.ToResponse(task);
    }
}
