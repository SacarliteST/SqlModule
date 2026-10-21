using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Progress;

public static class ProgressMappings
{
    public static StudentTaskProgressResponse ToResponse(
        StudentTaskProgress progress,
        TaskValidationVersion version,
        DateTimeOffset now)
    {
        int? remaining = version.MaxAttempts.HasValue
            ? Math.Max(0, version.MaxAttempts.Value - progress.AttemptsUsed)
            : null;
        var active = progress.Status == ProgressStatus.Active &&
                     (!progress.ExpiresAt.HasValue || progress.ExpiresAt.Value > now);

        return new StudentTaskProgressResponse(
            progress.Id,
            progress.TaskId,
            progress.Status.ToContract(),
            progress.ValidationVersionId,
            progress.BestScore,
            progress.AttemptsUsed,
            remaining,
            active && remaining != 0,
            active,
            progress.BestScore >= version.PassingScore,
            progress.ExpiresAt,
            progress.FinalScore,
            progress.FinalizationReason?.ToContract(),
            progress.FinalizedAt);
    }
}
