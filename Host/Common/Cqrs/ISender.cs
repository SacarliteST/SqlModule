namespace SQLModule.Host.Common.Cqrs;

/// <summary>
/// Точка входа в CQRS-пайплайн. Используется из эндпоинтов для отправки команд и запросов.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Отправляет запрос через пайплайн behaviors и возвращает ответ хендлера.
    /// </summary>
    /// <typeparam name="TCommand">Конкретный тип команды.</typeparam>
    /// <typeparam name="TResponse">Ожидаемый тип ответа.</typeparam>
    /// <param name="command">Данные запроса.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<TResponse> Send<TCommand, TResponse>(TCommand command, CancellationToken ct = default)
        where TCommand : IRequest<TResponse>;
}
