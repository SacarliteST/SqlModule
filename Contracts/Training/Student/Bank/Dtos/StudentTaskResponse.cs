namespace SQLModule.Contracts.Training.Student;

/// <summary>Карточка задания в каталоге открытого банка.</summary>
/// <param name="Id">Идентификатор задания.</param>
/// <param name="BankNumber">Постоянный номер задания в банке.</param>
/// <param name="TopicId">Идентификатор темы.</param>
/// <param name="TopicName">Название темы.</param>
/// <param name="TaskName">Название задания.</param>
/// <param name="TaskTextPreview">Краткий фрагмент условия.</param>
/// <param name="DifficultyLevel">Уровень сложности.</param>
/// <param name="DbmsName">Название СУБД.</param>
/// <param name="Status">Личный статус решения.</param>
/// <param name="AttemptsCount">Число тренировочных попыток после открытия в банке.</param>
/// <param name="BestScore">Лучший балл.</param>
/// <param name="LastAttemptAt">Дата последней тренировочной попытки.</param>
/// <param name="BankPublishedAt">Дата первого открытия в банке.</param>
public sealed record StudentTaskResponse(
    Guid Id,
    long BankNumber,
    Guid TopicId,
    string TopicName,
    string TaskName,
    string TaskTextPreview,
    short DifficultyLevel,
    string DbmsName,
    BankTaskStatus Status,
    int AttemptsCount,
    int? BestScore,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset BankPublishedAt);
