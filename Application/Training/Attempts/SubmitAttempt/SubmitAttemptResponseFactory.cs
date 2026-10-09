using SQLModule.Application.Training.Progress;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

/// <summary>Собирает ответ на отправку решения из сохранённых данных попытки и оценки.</summary>
public static class SubmitAttemptResponseFactory
{
    public static SubmitAttemptResponse Create(
        Attempt attempt,
        StudentTaskProgress progress,
        ValidationRuntime runtime,
        IReadOnlyList<EvaluatedCheck> checks,
        AttemptResultSnapshot snapshot,
        DateTimeOffset now)
    {
        var progressResponse = ProgressMappings.ToResponse(progress, runtime.Version, now);
        var visible = runtime.Configuration.VisibleHintGroups.ToHashSet();
        var visibleChecks = checks.Where(value =>
        {
            var group = AttemptScoringReadService.ToHintGroup(value.Check.Kind);
            return group.HasValue && visible.Contains(group.Value);
        }).ToArray();
        var hints = checks.Where(value => value.Status == ValidationCheckStatus.Failed)
            .Select(value => ToHint(value.Check.Kind))
            .Where(group => group.HasValue && visible.Contains(group.Value))
            .Distinct()
            .Select(group => new AttemptHintResponse(group!.Value, "Проверьте соответствующую часть решения."))
            .ToArray();
        return new SubmitAttemptResponse(
            attempt.Id, attempt.Status, attempt.IsCorrect, attempt.Reason,
            attempt.RowCount, attempt.DurationMs, attempt.ErrorMessage,
            snapshot.Columns, snapshot.Rows, snapshot.IsTruncated, snapshot.State,
            snapshot.ReturnedRowCount, snapshot.RowLimit, snapshot.CreatedAt, snapshot.ExpiresAt,
            progress.Id, runtime.Version.Id, attempt.AttemptNumber, attempt.Score,
            progress.BestScore, runtime.Version.PassingScore, progressResponse.IsPassed,
            progress.AttemptsUsed, progressResponse.AttemptsRemaining,
            progressResponse.CanSubmit, progressResponse.CanFinalize,
            visibleChecks.Select(value => new AttemptCheckResultResponse(
                value.Check.Kind, value.Status, value.Check.Weight, value.AwardedScore, value.Message)).ToArray(),
            hints);
    }

    private static HintGroup? ToHint(ValidationCheckKind kind) => kind switch
    {
        ValidationCheckKind.MainDatasetResult => HintGroup.Result,
        ValidationCheckKind.RequiredConstruct => HintGroup.RequiredConstructs,
        ValidationCheckKind.ForbiddenConstruct => HintGroup.ForbiddenConstructs,
        ValidationCheckKind.RequiredTable => HintGroup.RequiredTables,
        ValidationCheckKind.ForbiddenTable => HintGroup.ForbiddenTables,
        _ => null
    };
}
