using Microsoft.Extensions.DependencyInjection;
using SQLModule.Web.Common.Behaviors;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Common;

/// <summary>
/// Методы регистрации CQRS-ядра в DI.
/// </summary>
internal static class CqrsExtensions
{
    /// <summary>
    /// Регистрирует <see cref="ISender"/> и открытый generic <see cref="IPipelineBehavior{TRequest,TResponse}"/>
    /// с реализацией <see cref="LoggingBehavior{TRequest,TResponse}"/>.
    /// </summary>
    internal static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
