namespace SQLModule.Application.SpdIntegration;

/// <summary>Настройки интеграции SQL-модуля с СППР.</summary>
public sealed class EducationSpdOptions
{
    public const string SectionKey = "EducationSpd";

    public bool Enabled { get; init; }
    public string ModuleKey { get; init; } = "sql";
    public EducationSpdKafkaOptions Kafka { get; init; } = new();
}

public sealed class EducationSpdKafkaOptions
{
    public string SkillCatalogTopic { get; init; } = "learning.skill-catalog";
    public string EpisodeFinalizedTopic { get; init; } = "learning.episode-finalized";
}
