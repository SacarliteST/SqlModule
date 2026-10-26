namespace SQLModule.Contracts.Training.Validation;

/// <summary>Таблица, раскрытая студенту как безопасная подсказка.</summary>
public sealed record StudentHintTableResponse(Guid Id, string Name);
