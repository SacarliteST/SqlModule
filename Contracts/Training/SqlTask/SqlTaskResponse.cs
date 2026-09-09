namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Данные SQL-задания.</summary>
/// <param name="Id">Уникальный идентификатор задания.</param>
/// <param name="TopicId">Идентификатор темы.</param>
/// <param name="SqlQueryId">Идентификатор эталонного SQL-запроса.</param>
/// <param name="TaskName">Название задания.</param>
/// <param name="TaskText">Текст условия задания.</param>
/// <param name="DifficultyLevel">Уровень сложности (1–5).</param>
/// <param name="PublicationStatus">Статус публикации задания.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания (UTC).</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения (UTC).</param>
/// <param name="TopicName">Название темы для teacher-списка.</param>
/// <param name="TargetDbId">Идентификатор учебной базы эталона.</param>
/// <param name="TargetDbName">Название учебной базы эталона.</param>
/// <param name="DbmsName">Название СУБД.</param>
/// <param name="AttemptsCount">Общее число попыток.</param>
/// <param name="CreatedByName">Отображаемое имя автора на момент создания.</param>
/// <param name="UpdatedByName">Отображаемое имя последнего редактора.</param>
public record SqlTaskResponse(
    Guid Id,
    Guid TopicId,
    Guid SqlQueryId,
    string TaskName,
    string TaskText,
    short DifficultyLevel,
    SQLModule.Domain.Training.PublicationStatus PublicationStatus,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt,
    string? TopicName = null,
    Guid? TargetDbId = null,
    string? TargetDbName = null,
    string? DbmsName = null,
    int AttemptsCount = 0,
    string? CreatedByName = null,
    string? UpdatedByName = null);
