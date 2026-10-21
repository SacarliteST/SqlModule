namespace SQLModule.Application.Cqrs;

/// <summary>
/// Маркерный интерфейс запроса в CQRS-пайплайне.
/// </summary>
/// <typeparam name="TResponse">Тип возвращаемого ответа.</typeparam>
public interface IRequest<TResponse>;
