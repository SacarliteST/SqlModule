using SQLModule.Contracts.Training.Topic;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Topics;

internal static class TopicMappings
{
    internal static TopicResponse ToResponse(Topic e) => new(
        e.Id, e.TopicName, e.ParentTopicId, e.Description,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt,
        e.CreatedByName, e.UpdatedByName);

    internal static CreateTopicCommand ToCommand(CreateTopicRequest req) =>
        new(req.TopicName, req.ParentTopicId, req.Description);

    internal static UpdateTopicCommand ToCommand(Guid id, UpdateTopicRequest req) =>
        new(id, req.TopicName, req.Description);

    internal static MoveTopicCommand ToCommand(Guid id, MoveTopicRequest req) =>
        new(id, req.ParentTopicId);
}
