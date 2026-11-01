using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Topics;

public record CreateTopicCommand(string TopicName, Guid? ParentTopicId, string? Description)
    : IRequest<Result<TopicResponse>>;

public sealed class CreateTopicHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTopicCommand, Result<TopicResponse>>
{
    public async Task<Result<TopicResponse>> Handle(CreateTopicCommand command, CancellationToken ct)
    {
        if (command.ParentTopicId.HasValue &&
            !await db.Topics.AnyAsync(t => t.Id == command.ParentTopicId.Value, ct))
        {
            return Result<TopicResponse>.Fail(TopicErrors.ParentNotFound(command.ParentTopicId.Value));
        }

        var entity = Topic.Create(command.TopicName, command.ParentTopicId, description: command.Description);
        db.Topics.Add(entity);
        await db.SaveChangesAsync(ct);
        return TopicMappings.ToResponse(entity);
    }
}
