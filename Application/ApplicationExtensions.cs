using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Behaviors;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application;

/// <summary>Точка подключения прикладного слоя: CQRS-ядро и обработчики сценариев.</summary>
public static class ApplicationExtensions
{
    /// <summary>Регистрирует <see cref="ISender"/>, pipeline-поведения и обработчики сценариев.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
