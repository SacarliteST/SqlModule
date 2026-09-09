namespace SQLModule.Web.Common.Isolated;

/// <summary>Настройки импорта учебного курса из JSON при старте приложения.</summary>
internal sealed class CourseSeedOptions
{
    internal const string SectionKey = "CourseSeed";

    /// <summary>Включить импорт курса после применения миграций.</summary>
    public bool Enabled { get; init; }

    /// <summary>Абсолютный путь либо путь относительно content root проекта Host.</summary>
    public string FilePath { get; init; } = String.Empty;
}
