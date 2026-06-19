using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

internal record CreateTopicCommand(string TopicName, Guid? ParentTopicId)
    : IRequest<Result<TopicResponse>>;

internal sealed class CreateTopicHandler(AppDbContext db)
    : IRequestHandler<CreateTopicCommand, Result<TopicResponse>>
{
    public async Task<Result<TopicResponse>> Handle(CreateTopicCommand command, CancellationToken ct)
    {
        if (command.ParentTopicId.HasValue &&
            !await db.Topics.AnyAsync(t => t.Id == command.ParentTopicId.Value, ct))
        {
            return Result<TopicResponse>.Fail(Error.Conflict(
                "Topic.ParentNotFound",
                $"Родительская тема с id '{command.ParentTopicId}' не найдена."));
        }

        var entity = Topic.Create(command.TopicName, command.ParentTopicId);
        db.Topics.Add(entity);
        await db.SaveChangesAsync(ct);
        return TopicMappings.ToResponse(entity);
    }
}
