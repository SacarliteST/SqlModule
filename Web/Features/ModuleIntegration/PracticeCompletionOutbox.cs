using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.PlatformIntegration.Contracts;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class PracticeCompletionOutbox(AppDbContext db)
{
    internal async Task<bool> TryAddAsync(
        ModuleSession session,
        int finalGrade,
        int completionAttemptCount,
        FinalizationReason reason,
        DateTimeOffset finalizedAt,
        Attempt? currentAttempt,
        CancellationToken ct)
    {
        var gradeKey = $"grade:{session.Id:D}";
        var gradeExists = db.PendingPublishes.Local.Any(value => value.DeduplicationKey == gradeKey) ||
                          await db.PendingPublishes.AsNoTracking()
                              .AnyAsync(value => value.DeduplicationKey == gradeKey, ct);
        if (gradeExists)
        {
            return false;
        }

        var storedCount = await db.Attempts.AsNoTracking()
            .CountAsync(value => value.ModuleSessionId == session.Id, ct);
        var lastFinishedAt = await db.Attempts.AsNoTracking()
            .Where(value => value.ModuleSessionId == session.Id)
            .MaxAsync(value => (DateTimeOffset?)value.FinishedAt, ct);
        var includeExtremes = reason is not (FinalizationReason.Expired or FinalizationReason.EducationClosed);
        var best = await LoadExtremeAsync(session.Id, descending: true, ct);
        PracticeSummaryAttempt? worst = null;
        if (includeExtremes)
        {
            worst = await LoadExtremeAsync(session.Id, descending: false, ct);
        }
        if (currentAttempt?.CountsTowardLimit == true)
        {
            var current = ToSummaryAttempt(currentAttempt);
            best = ChooseBest(best, current);
            if (includeExtremes)
            {
                worst = ChooseWorst(worst, current);
            }
        }

        var totalAttempts = storedCount + (currentAttempt is null ? 0 : 1);
        var latest = currentAttempt is null || lastFinishedAt >= currentAttempt.FinishedAt
            ? lastFinishedAt
            : currentAttempt.FinishedAt;
        var occurredAt = latest.HasValue && finalizedAt <= latest.Value
            ? latest.Value.AddMilliseconds(1)
            : finalizedAt;
        var durationMs = session.CreatedAt == default
            ? null
            : (long?)Math.Max(0, (finalizedAt - session.CreatedAt).TotalMilliseconds);

        var completion = new PracticeCompletionRequest(
            session.SessionKey,
            finalGrade,
            new PracticeCompletionData(completionAttemptCount, best?.Id ?? currentAttempt?.Id ?? Guid.Empty),
            finalizedAt);
        db.PendingPublishes.Add(PendingPublish.Create(
            Guid.NewGuid(), PendingPublishKind.Grade, session.Id, gradeKey,
            JsonSerializer.Serialize(completion, PlatformIntegrationJson.Default), finalizedAt));

        var eventId = Guid.NewGuid();
        var payload = new PracticeSessionSummaryPayload(
            totalAttempts, finalGrade, reason.ToString(), durationMs);
        var report = new PracticeSessionSummaryReport(
            totalAttempts, finalGrade, reason, durationMs, includeExtremes ? best : null, worst);
        var message = new PracticeEventMessage(
            session.Id,
            session.SessionKey,
            eventId,
            "session_summary",
            occurredAt,
            JsonSerializer.SerializeToElement(payload, PlatformIntegrationJson.Default),
            PracticeEventMarkdownBuilder.BuildSummary(report));
        db.PendingPublishes.Add(PendingPublish.Create(
            eventId, PendingPublishKind.Event, session.Id, $"summary:{session.Id:D}",
            JsonSerializer.Serialize(message, PlatformIntegrationJson.Default), occurredAt));
        return true;
    }

    private async Task<PracticeSummaryAttempt?> LoadExtremeAsync(
        Guid sessionId,
        bool descending,
        CancellationToken ct)
    {
        var attempts = db.Attempts.AsNoTracking()
            .Where(value => value.ModuleSessionId == sessionId && value.CountsTowardLimit);
        var ordered = descending
            ? attempts.OrderByDescending(value => value.Score ?? (value.IsCorrect ? 100 : 0))
            : attempts.OrderBy(value => value.Score ?? (value.IsCorrect ? 100 : 0));
        return await ordered
            .ThenBy(value => value.AttemptNumber ?? Int32.MaxValue)
            .ThenBy(value => value.StartedAt)
            .Select(value => new PracticeSummaryAttempt(
                value.Id,
                value.AttemptNumber,
                value.Score ?? (value.IsCorrect ? 100 : 0),
                value.IsCorrect,
                value.Reason,
                value.DurationMs,
                value.StartedAt))
            .FirstOrDefaultAsync(ct);
    }

    private static PracticeSummaryAttempt ToSummaryAttempt(Attempt attempt) => new(
        attempt.Id,
        attempt.AttemptNumber,
        attempt.Score ?? (attempt.IsCorrect ? 100 : 0),
        attempt.IsCorrect,
        attempt.Reason,
        attempt.DurationMs,
        attempt.StartedAt);

    private static PracticeSummaryAttempt ChooseBest(
        PracticeSummaryAttempt? stored,
        PracticeSummaryAttempt current) => stored is null || Compare(current, stored) < 0 ? current : stored;

    private static PracticeSummaryAttempt ChooseWorst(
        PracticeSummaryAttempt? stored,
        PracticeSummaryAttempt current) => stored is null || CompareWorst(current, stored) < 0 ? current : stored;

    private static int Compare(PracticeSummaryAttempt left, PracticeSummaryAttempt right)
    {
        var score = right.Score.CompareTo(left.Score);
        return score != 0 ? score : CompareTie(left, right);
    }

    private static int CompareWorst(PracticeSummaryAttempt left, PracticeSummaryAttempt right)
    {
        var score = left.Score.CompareTo(right.Score);
        return score != 0 ? score : CompareTie(left, right);
    }

    private static int CompareTie(PracticeSummaryAttempt left, PracticeSummaryAttempt right)
    {
        var number = (left.AttemptNumber ?? Int32.MaxValue).CompareTo(right.AttemptNumber ?? Int32.MaxValue);
        return number != 0 ? number : left.StartedAt.CompareTo(right.StartedAt);
    }
}
