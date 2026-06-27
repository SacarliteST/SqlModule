namespace SQLModule.Web.Common.Cqrs;

/// <summary>
/// Делегат следующего шага в цепочке pipeline-behaviors.
/// </summary>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
/// <param name="ct">Токен отмены.</param>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken ct);

/// <summary>
/// Pipeline-поведение, оборачивающее выполнение хендлера.
/// Реализации выполняются в порядке регистрации (первый зарегистрированный — внешний).
/// </summary>
/// <typeparam name="TRequest">Тип запроса.</typeparam>
/// <typeparam name="TResponse">Тип ответа.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Выполняет логику поведения и передаёт управление следующему звену цепочки.
    /// </summary>
    /// <param name="request">Данные запроса.</param>
    /// <param name="next">Следующий шаг пайплайна.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
