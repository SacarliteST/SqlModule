using Microsoft.Extensions.DependencyInjection;

namespace SQLModule.Client;

/// <summary>
/// Методы регистрации клиентской инфраструктуры в DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует базовую HTTP-инфраструктуру клиента (<see cref="ErrorDelegatingHandler"/>).
    /// Конкретные typed-clients (IXxxClient) регистрируются в отдельных методах расширения.
    /// </summary>
    public static IServiceCollection AddClient(this IServiceCollection services)
    {
        services.AddTransient<ErrorDelegatingHandler>();
        return services;
    }
}
