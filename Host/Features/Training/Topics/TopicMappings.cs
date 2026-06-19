using SQLModule.Contracts.Training.Topic;
using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.Topics;

internal static class TopicMappings
{
    internal static TopicResponse ToResponse(Topic e) => new(
        e.Id, e.TopicName, e.ParentTopicId,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateTopicCommand ToCommand(CreateTopicRequest req) =>
        new(req.TopicName, req.ParentTopicId);

    internal static UpdateTopicCommand ToCommand(Guid id, UpdateTopicRequest req) =>
        new(id, req.TopicName);

    internal static MoveTopicCommand ToCommand(Guid id, MoveTopicRequest req) =>
        new(id, req.ParentTopicId);
}
