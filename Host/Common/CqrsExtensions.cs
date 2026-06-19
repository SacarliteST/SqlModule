using SQLModule.Host.Common.Behaviors;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Common;

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
