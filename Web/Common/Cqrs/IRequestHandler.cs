namespace SQLModule.Web.Common.Cqrs;

/// <summary>
/// Хендлер запроса. Содержит бизнес-логику обработки одной команды или запроса.
/// </summary>
/// <typeparam name="TRequest">Тип команды или запроса, реализующий <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого ответа.</typeparam>
public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>Обрабатывает запрос и возвращает ответ.</summary>
    /// <param name="request">Данные запроса.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
