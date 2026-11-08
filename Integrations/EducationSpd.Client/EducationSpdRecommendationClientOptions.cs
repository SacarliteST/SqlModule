namespace SQLModule.EducationSpd.Client;

/// <summary>Настройки HTTP-клиента рекомендаций EducationSPD.</summary>
public sealed class EducationSpdRecommendationClientOptions
{
    public const string SectionKey = "EducationSpd:Recommendations";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = String.Empty;
    public string ServiceKey { get; init; } = String.Empty;
    public int RequestTimeoutSeconds { get; init; } = 2;
}
