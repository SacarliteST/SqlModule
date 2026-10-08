using SQLModule.Application.Training.Progress;
﻿using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Application.Persistence;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Attempts;

public interface IAttemptScoringReadService
{
    Task<AttemptScoringResponse?> ReadAsync(Attempt attempt, bool studentSafe, CancellationToken ct);
}

public sealed class AttemptScoringReadService(
    IApplicationDbContext db,
    TimeProvider timeProvider) : IAttemptScoringReadService
{
    public async Task<AttemptScoringResponse?> ReadAsync(
        Attempt attempt,
        bool studentSafe,
        CancellationToken ct)
    {
        if (!attempt.ProgressId.HasValue || !attempt.ValidationVersionId.HasValue ||
            !attempt.AttemptNumber.HasValue || !attempt.Score.HasValue)
        {
            return null;
        }

        var progress = await db.StudentTaskProgresses.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == attempt.ProgressId.Value, ct);
        var version = await db.TaskValidationVersions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == attempt.ValidationVersionId.Value, ct);
        if (progress is null || version is null)
        {
            return null;
        }

        var visibleGroups = version.GetVisibleHintGroups().ToHashSet();
        var storedChecks = await db.AttemptCheckResults.AsNoTracking()
            .Where(value => value.AttemptId == attempt.Id)
            .OrderBy(value => value.Order)
            .ToArrayAsync(ct);
        var checks = storedChecks
            .Where(value => !studentSafe || IsVisible(value.Kind, visibleGroups))
            .Select(value => new AttemptCheckResultResponse(
                value.Kind, value.Status, value.Weight, value.AwardedScore, value.Message))
            .ToArray();
        var hints = studentSafe
            ? storedChecks
                .Where(value => value.Status == ValidationCheckStatus.Failed)
                .Select(value => ToHintGroup(value.Kind))
                .Where(value => value.HasValue && visibleGroups.Contains(value.Value))
                .Distinct()
                .Select(value => new AttemptHintResponse(
                    value!.Value, "Проверьте соответствующую часть решения."))
                .ToArray()
            : [];
        var state = ProgressMappings.ToResponse(progress, version, timeProvider.GetUtcNow());

        return new AttemptScoringResponse(
            attempt.Id,
            progress.Id,
            version.Id,
            attempt.AttemptNumber.Value,
            attempt.Score.Value,
            progress.BestScore,
            version.PassingScore,
            state.IsPassed,
            progress.AttemptsUsed,
            state.AttemptsRemaining,
            state.CanSubmit,
            state.CanFinalize,
            checks,
            hints);
    }

    public static HintGroup? ToHintGroup(ValidationCheckKind kind) => kind switch
    {
        ValidationCheckKind.MainDatasetResult => HintGroup.Result,
        ValidationCheckKind.RequiredConstruct => HintGroup.RequiredConstructs,
        ValidationCheckKind.ForbiddenConstruct => HintGroup.ForbiddenConstructs,
        ValidationCheckKind.RequiredTable => HintGroup.RequiredTables,
        ValidationCheckKind.ForbiddenTable => HintGroup.ForbiddenTables,
        _ => null
    };

    private static bool IsVisible(ValidationCheckKind kind, IReadOnlySet<HintGroup> visibleGroups)
    {
        var group = ToHintGroup(kind);
        return group.HasValue && visibleGroups.Contains(group.Value);
    }
}
