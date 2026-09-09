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
    PublicationStatus? PublicationStatus,
    Guid? TopicId)
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

        var topicId = command.TopicId ?? entity.TopicId;
        var topicChanged = topicId != entity.TopicId;

        if (topicChanged)
        {
            if (entity.PublicationStatus != PublicationStatus.Draft)
            {
                return Result.Fail(SqlTaskErrors.LinksChangeRequiresDraft(command.Id));
            }

            if (await db.Attempts.AnyAsync(x => x.TaskId == command.Id, ct))
            {
                return Result.Fail(SqlTaskErrors.LinksChangeBlockedByAttempts(command.Id));
            }

            if (topicChanged && !await db.Topics.AnyAsync(x => x.Id == topicId, ct))
            {
                return Result.Fail(SqlTaskErrors.TopicNotFound(topicId));
            }

            if (topicChanged)
            {
                entity.ChangeTopic(topicId);
            }
        }

        entity.Update(command.TaskName, command.TaskText, command.DifficultyLevel, command.PublicationStatus);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
