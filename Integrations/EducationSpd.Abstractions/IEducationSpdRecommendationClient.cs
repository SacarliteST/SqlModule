using EducationSPD.Contracts.Recommendations;

namespace SQLModule.EducationSpd.Abstractions;

/// <summary>Результат безопасного обращения к сервису ранжирования EducationSPD.</summary>
public sealed record RecommendationClientResult(
    bool IsSuccess,
    RankRecommendationsResponse? Response,
    string? FailureCode)
{
    public static RecommendationClientResult Success(RankRecommendationsResponse response) =>
        new(true, response, null);

    public static RecommendationClientResult Unavailable(string failureCode) =>
        new(false, null, failureCode);
}

/// <summary>Ранжирует уже разрешённый SQL-модулем набор заданий.</summary>
public interface IEducationSpdRecommendationClient
{
    Task<RecommendationClientResult> RankAsync(
        RankRecommendationsRequest request,
        CancellationToken ct = default);
}
