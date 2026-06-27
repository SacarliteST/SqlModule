using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SQLModule.Contracts;

namespace SQLModule.Web.Common;

/// <summary>
/// Методы регистрации и маппинга эндпоинтов через рефлексию по сборке Host.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Сканирует сборку и регистрирует все конкретные реализации <see cref="IEndpoint"/> как Transient.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointTypes = typeof(EndpointExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), type);
        }

        return services;
    }

    /// <summary>
    /// Создаёт временный DI-scope, резолвит все <see cref="IEndpoint"/> и вызывает
    /// <see cref="IEndpoint.MapEndpoints"/> для каждого.
    /// </summary>
    /// <param name="app">Экземпляр <see cref="WebApplication"/>.</param>
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var devEnabled = app.Environment.IsDevelopment()
                         || app.Configuration.GetValue<bool>("DevTools:Enabled");

        using var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            if (endpoint is IDevEndpoint && !devEnabled)
            {
                continue;
            }

            endpoint.MapEndpoints(app);
        }

        return app;
    }
}
