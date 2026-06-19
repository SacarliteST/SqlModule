namespace SQLModule.Contracts.Training.Topic;

/// <summary>Запрос на обновление названия темы.</summary>
/// <param name="TopicName">Новое название темы (не пустое, не более 300 символов).</param>
public record UpdateTopicRequest(string TopicName);
