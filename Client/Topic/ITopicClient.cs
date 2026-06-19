using SQLModule.Contracts.Training.Topic;

namespace SQLModule.Client.Topic;

/// <summary>Клиент для работы с темами тренажёра.</summary>
public interface ITopicClient : ICrudClient<CreateTopicRequest, UpdateTopicRequest, TopicResponse>
{
    /// <summary>Перемещает тему в иерархии (меняет родителя).</summary>
    /// <param name="id">Идентификатор темы.</param>
    /// <param name="request">Параметры перемещения (новый родитель или <c>null</c> для корневой).</param>
    /// <param name="ct">Токен отмены.</param>
    /// <exception cref="NotFoundException">Тема не найдена.</exception>
    /// <exception cref="ConflictException">Новый родитель не найден или перемещение создаёт цикл.</exception>
    Task MoveAsync(Guid id, MoveTopicRequest request, CancellationToken ct = default);
}
