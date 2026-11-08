using System.Text.Json;
using EducationSPD.Contracts.Recommendations;
using Microsoft.Extensions.Logging;
using SQLModule.EducationSpd.Abstractions;

namespace SQLModule.Application.SpdIntegration;

/// <summary>Кандидат, полностью отфильтрованный SQL-модулем до обращения к EducationSPD.</summary>
public sealed record StudentRecommendationCandidate(
    Guid TaskId,
    short DifficultyLevel,
    string ValidationSnapshotJson);

/// <summary>Применяет персональное ранжирование либо сохраняет локальный порядок.</summary>
public sealed class StudentRecommendationRanker(
    ILogger<StudentRecommendationRanker> logger,
    IEducationSpdRecommendationClient? client = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Guid>> RankAsync(
        Guid studentId,
        IReadOnlyList<StudentRecommendationCandidate> candidates,
        CancellationToken ct)
    {
        var fallback = candidates.Select(candidate => candidate.TaskId).ToArray();
        if (client is null || candidates.Count == 0 || candidates.Count > 500)
        {
            if (candidates.Count > 500)
            {
                logger.LogWarning(
                    "Локальный fallback рекомендаций применён: число кандидатов {CandidateCount} превышает 500",
                    candidates.Count);
            }

            return fallback;
        }

        var requestCandidates = candidates
            .Select(ToRequest)
            .Where(candidate => candidate is not null)
            .Cast<RecommendationCandidateRequest>()
            .ToArray();
        if (requestCandidates.Length == 0)
        {
            return fallback;
        }

        var result = await client.RankAsync(
            new RankRecommendationsRequest(studentId, "sql", requestCandidates), ct);
        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "Применён локальный fallback рекомендаций; безопасный код {FailureCode}",
                result.FailureCode);
            return fallback;
        }

        var rankedIds = result.Response!.Items.Select(item => item.TaskId).ToArray();
        var rankedSet = rankedIds.ToHashSet();
        return [.. rankedIds, .. fallback.Where(taskId => !rankedSet.Contains(taskId))];
    }

    private static RecommendationCandidateRequest? ToRequest(StudentRecommendationCandidate candidate)
    {
        ValidationSkillSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<ValidationSkillSnapshot>(
                candidate.ValidationSnapshotJson,
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        var skills = (snapshot?.Checks ?? [])
            .Where(check => check.Skill is not null)
            .Select(check => new RecommendationSkillRequest(
                check.Skill!.SkillId,
                RecommendationSkillSource.ValidationRule))
            .Concat((snapshot?.SkillTags ?? []).Select(tag => new RecommendationSkillRequest(
                tag.SkillId,
                RecommendationSkillSource.TaskTag)))
            .GroupBy(skill => skill.SkillId, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        return skills.Length == 0
            ? null
            : new RecommendationCandidateRequest(candidate.TaskId, candidate.DifficultyLevel, skills);
    }

    private sealed record ValidationSkillSnapshot(
        IReadOnlyList<ValidationCheckSkillSnapshot>? Checks,
        IReadOnlyList<SkillSnapshot>? SkillTags);

    private sealed record ValidationCheckSkillSnapshot(SkillSnapshot? Skill);

    private sealed record SkillSnapshot(string SkillId);
}
