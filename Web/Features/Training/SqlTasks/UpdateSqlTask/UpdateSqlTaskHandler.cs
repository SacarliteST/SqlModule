using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal record UpdateSqlTaskCommand(
    Guid Id,
    string TaskName,
    string TaskText,
    short DifficultyLevel,
    PublicationStatus? PublicationStatus)
    : IRequest<Result>;

internal sealed class UpdateSqlTaskHandler(AppDbContext db)
    : IRequestHandler<UpdateSqlTaskCommand, Result>
{
    public async Task<Result> Handle(UpdateSqlTaskCommand command, CancellationToken ct)
    {
        var entity = await db.SqlTasks.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(SqlTaskErrors.NotFound(command.Id));
        }

        if (command.PublicationStatus == PublicationStatus.Published &&
            entity.PublicationStatus != PublicationStatus.Published)
        {
            return Result.Fail(SqlTaskErrors.PublishRequiresAction);
        }

        entity.Update(command.TaskName, command.TaskText, command.DifficultyLevel, command.PublicationStatus);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
