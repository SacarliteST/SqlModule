using SQLModule.Contracts.Training.Attempt;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Attempts;

internal static class AttemptMappings
{
    internal static AttemptResponse ToResponse(Attempt e) => new(
        e.Id, e.UserId, e.StudentName, e.TaskId, e.SubmittedSql,
        e.Status, e.IsCorrect, e.Reason,
        e.RowCount, e.DurationMs, e.ErrorMessage,
        e.StartedAt, e.FinishedAt,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
