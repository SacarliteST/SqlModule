namespace SQLModule.Application.Seeding;

/// <summary>Настройки импорта учебного курса из JSON при старте приложения.</summary>
public sealed class CourseSeedOptions
{
    public const string SectionKey = "CourseSeed";

    /// <summary>Включить импорт курса после применения миграций.</summary>
    public bool Enabled { get; init; }

    /// <summary>Абсолютный путь либо путь относительно content root проекта Host.</summary>
    public string FilePath { get; init; } = String.Empty;
}
