using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Bank;
using SQLModule.Contracts.Training.Validation;
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
        if (!attempt.ValidationVersionId.HasValue ||
            !attempt.AttemptNumber.HasValue || !attempt.Score.HasValue)
        {
            return null;
        }

        var version = await db.TaskValidationVersions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == attempt.ValidationVersionId.Value, ct);
        if (version is null)
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
                value.Kind.ToContract(), value.Status.ToContract(), value.Weight, value.AwardedScore, value.Message))
            .ToArray();
        var hints = studentSafe
            ? storedChecks
                .Where(value => value.Status == ValidationCheckStatus.Failed)
                .Select(value => TaskBankHintRules.ToHintGroup(value.Kind))
                .Where(value => value.HasValue && visibleGroups.Contains(value.Value))
                .Distinct()
                .Select(value => new AttemptHintResponse(
                    value!.Value.ToContract(), "Проверьте соответствующую часть решения."))
                .ToArray()
            : [];
        var attempts = db.Attempts.AsNoTracking().Where(value => value.CountsTowardLimit &&
            (attempt.ModuleSessionId.HasValue
                ? value.ModuleSessionId == attempt.ModuleSessionId
                : value.ModuleSessionId == null && value.UserId == attempt.UserId && value.TaskId == attempt.TaskId));
        var attemptsUsed = await attempts.CountAsync(ct);
        var bestScore = await attempts.MaxAsync(value => (int?)value.Score, ct) ?? attempt.Score.Value;
        var session = attempt.ModuleSessionId.HasValue
            ? await db.ModuleSessions.AsNoTracking().SingleOrDefaultAsync(
                value => value.Id == attempt.ModuleSessionId.Value, ct)
            : null;
        var maxAttempts = session?.MaxAttempts;
        int? attemptsRemaining = maxAttempts.HasValue
            ? Math.Max(0, maxAttempts.Value - attemptsUsed)
            : null;
        var active = session is null || session.Status == Domain.ModuleIntegration.ModuleSessionStatus.Active &&
            !session.IsExpired(timeProvider.GetUtcNow());

        return new AttemptScoringResponse(
            attempt.Id,
            version.Id,
            attempt.AttemptNumber.Value,
            attempt.Score.Value,
            bestScore,
            version.PassingScore,
            bestScore >= version.PassingScore,
            attemptsUsed,
            attemptsRemaining,
            active && attemptsRemaining != 0,
            session is not null && active,
            checks,
            hints);
    }

    private static bool IsVisible(ValidationCheckKind kind, IReadOnlySet<HintGroup> visibleGroups)
    {
        var group = TaskBankHintRules.ToHintGroup(kind);
        return group.HasValue && visibleGroups.Contains(group.Value);
    }
}
