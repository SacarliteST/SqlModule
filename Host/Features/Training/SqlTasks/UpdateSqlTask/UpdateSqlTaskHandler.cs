using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal record UpdateSqlTaskCommand(Guid Id, string TaskName, string TaskText, short DifficultyLevel)
    : IRequest<Result>;

internal sealed class UpdateSqlTaskHandler(AppDbContext db)
    : IRequestHandler<UpdateSqlTaskCommand, Result>
{
    public async Task<Result> Handle(UpdateSqlTaskCommand command, CancellationToken ct)
    {
        var entity = await db.SqlTasks.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(Error.NotFound("SqlTask", command.Id));
        }

        entity.Update(command.TaskName, command.TaskText, command.DifficultyLevel);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
