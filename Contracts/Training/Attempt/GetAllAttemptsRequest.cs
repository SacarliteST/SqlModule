namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Параметры запроса списка попыток с пагинацией.</summary>
/// <param name="Offset">Количество пропускаемых записей. Минимум: 0. По умолчанию: 0.</param>
/// <param name="Limit">Максимальное количество возвращаемых записей. Диапазон: 1–100. По умолчанию: 20.</param>
/// <param name="TaskId">Необязательный фильтр по заданию.</param>
/// <param name="UserId">Необязательный фильтр по студенту.</param>
/// <param name="TopicId">Необязательный фильтр по теме.</param>
/// <param name="Status">Необязательный фильтр по статусу выполнения.</param>
/// <param name="IsCorrect">Необязательный фильтр по корректности результата.</param>
/// <param name="DateFrom">Нижняя граница даты начала.</param>
/// <param name="DateTo">Верхняя граница даты начала.</param>
/// <param name="ProgressId">Необязательный фильтр по прохождению.</param>
/// <param name="ValidationVersionId">Необязательный фильтр по версии проверки.</param>
/// <param name="ScoreFrom">Минимальный балл включительно, от 0 до 100.</param>
/// <param name="ScoreTo">Максимальный балл включительно, от 0 до 100.</param>
/// <param name="FinalizationReason">Причина финализации связанного прохождения.</param>
public record GetAllAttemptsRequest(
    int Offset = 0,
    int Limit = 20,
    Guid? TaskId = null,
    Guid? UserId = null,
    Guid? TopicId = null,
    SQLModule.Domain.Training.ExecutionStatus? Status = null,
    bool? IsCorrect = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    Guid? ProgressId = null,
    Guid? ValidationVersionId = null,
    int? ScoreFrom = null,
    int? ScoreTo = null,
    SQLModule.Domain.Training.FinalizationReason? FinalizationReason = null);
