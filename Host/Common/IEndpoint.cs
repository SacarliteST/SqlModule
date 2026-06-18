namespace SQLModule.Host.Common;

/// <summary>
/// Контракт вертикального слайса. Каждый класс-эндпоинт реализует этот интерфейс
/// и регистрирует свои маршруты в <see cref="MapEndpoints"/>.
/// Все реализации обнаруживаются автоматически через <see cref="EndpointExtensions.AddEndpoints"/>.
/// </summary>
public interface IEndpoint
{
    /// <summary>Регистрирует маршруты эндпоинта в <paramref name="app"/>.</summary>
    /// <param name="app">Построитель маршрутов приложения.</param>
    void MapEndpoints(IEndpointRouteBuilder app);
}
