using SQLModule.Contracts.Training.Topic;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Topics;

public static class TopicMappings
{
    public static TopicResponse ToResponse(Topic e) => new(
        e.Id, e.TopicName, e.ParentTopicId, e.Description,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt,
        e.CreatedByName, e.UpdatedByName);

    public static CreateTopicCommand ToCommand(CreateTopicRequest req) =>
        new(req.TopicName, req.ParentTopicId, req.Description);

    public static UpdateTopicCommand ToCommand(Guid id, UpdateTopicRequest req) =>
        new(id, req.TopicName, req.Description);

    public static MoveTopicCommand ToCommand(Guid id, MoveTopicRequest req) =>
        new(id, req.ParentTopicId);
}
