namespace SQLModule.Contracts.Training.Topic;

/// <summary>Запрос на перемещение темы в иерархии.</summary>
/// <param name="ParentTopicId">Идентификатор нового родителя. <c>null</c> — сделать корневой темой.</param>
public record MoveTopicRequest(Guid? ParentTopicId);
