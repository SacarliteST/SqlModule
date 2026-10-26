namespace SQLModule.Application.Training.Student;

/// <summary>Проекция задания открытого банка с личной статистикой студента.</summary>
internal sealed class CatalogItem
{
    /// <summary>Идентификатор задания.</summary>
    public Guid Id { get; init; }

    /// <summary>Постоянный номер задания в банке.</summary>
    public long BankNumber { get; init; }

    /// <summary>Идентификатор темы.</summary>
    public Guid TopicId { get; init; }

    /// <summary>Название темы.</summary>
    public string TopicName { get; init; } = null!;

    /// <summary>Название задания.</summary>
    public string TaskName { get; init; } = null!;

    /// <summary>Условие задания.</summary>
    public string TaskText { get; init; } = null!;

    /// <summary>Уровень сложности.</summary>
    public short DifficultyLevel { get; init; }

    /// <summary>Название СУБД.</summary>
    public string DbmsName { get; init; } = null!;

    /// <summary>Дата первого открытия задания в банке.</summary>
    public DateTimeOffset BankPublishedAt { get; init; }

    /// <summary>Количество тренировочных попыток после открытия задания.</summary>
    public int AttemptsCount { get; init; }

    /// <summary>Лучший балл студента.</summary>
    public int? BestScore { get; init; }

    /// <summary>Дата последней тренировочной попытки.</summary>
    public DateTimeOffset? LastAttemptAt { get; init; }

    /// <summary>Достигал ли студент проходного балла.</summary>
    public bool Solved { get; init; }
}
