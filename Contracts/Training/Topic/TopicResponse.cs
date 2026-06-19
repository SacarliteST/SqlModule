namespace SQLModule.Contracts.Training.Topic;

/// <summary>Данные темы тренажёра.</summary>
/// <param name="Id">Уникальный идентификатор темы.</param>
/// <param name="TopicName">Название темы.</param>
/// <param name="ParentTopicId">Идентификатор родительской темы (<c>null</c> — корневая тема).</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания (UTC).</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения (UTC).</param>
public record TopicResponse(
    Guid Id,
    string TopicName,
    Guid? ParentTopicId,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
