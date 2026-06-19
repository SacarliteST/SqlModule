using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.Attempts;

internal static class AttemptMappings
{
    internal static AttemptResponse ToResponse(Attempt e) => new(
        e.Id, e.UserId, e.IsSuccess, e.StartAttempt, e.EndAttempt, e.TaskId, e.QueryId,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateAttemptCommand ToCommand(CreateAttemptRequest req, Guid userId) =>
        new(userId, req.IsSuccess, req.StartAttempt, req.EndAttempt, req.TaskId, req.QueryId);

    internal static UpdateAttemptCommand ToCommand(Guid id, UpdateAttemptRequest req) =>
        new(id, req.IsSuccess, req.EndAttempt);
}
