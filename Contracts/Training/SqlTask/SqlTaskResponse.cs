namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Данные SQL-задания.</summary>
/// <param name="Id">Уникальный идентификатор задания.</param>
/// <param name="TopicId">Идентификатор темы.</param>
/// <param name="SqlQueryId">Идентификатор эталонного SQL-запроса.</param>
/// <param name="TaskName">Название задания.</param>
/// <param name="TaskText">Текст условия задания.</param>
/// <param name="DifficultyLevel">Уровень сложности (1–5).</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания (UTC).</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения (UTC).</param>
public record SqlTaskResponse(
    Guid Id,
    Guid TopicId,
    Guid SqlQueryId,
    string TaskName,
    string TaskText,
    short DifficultyLevel,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
