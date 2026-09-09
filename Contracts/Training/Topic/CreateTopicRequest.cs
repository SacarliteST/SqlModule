namespace SQLModule.Contracts.Training.Topic;

/// <summary>Запрос на создание темы тренажёра.</summary>
/// <param name="TopicName">Название темы (не пустое, не более 300 символов).</param>
/// <param name="ParentTopicId">Идентификатор родительской темы. <c>null</c> — корневая тема.</param>
/// <param name="Description">Описание темы.</param>
public record CreateTopicRequest(string TopicName, Guid? ParentTopicId, string? Description = null);
