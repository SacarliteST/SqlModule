using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

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
            return Result<TopicResponse>.Fail(TopicErrors.ParentNotFound(command.ParentTopicId.Value));
        }

        var entity = Topic.Create(command.TopicName, command.ParentTopicId);
        db.Topics.Add(entity);
        await db.SaveChangesAsync(ct);
        return TopicMappings.ToResponse(entity);
    }
}
