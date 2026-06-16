using Microsoft.Extensions.DependencyInjection;
using SQLModule.Client.Configurations;
using SQLModule.Client.Template;

namespace SQLModule.Client;

/// <summary>
/// Добавляет методы расширения для регистрации клиентов
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация компонентов
    /// </summary>
    public static IServiceCollection AddClient(
        this IServiceCollection services,
        Action<TemplateClientOptions>? configureOptions = null
    )
    {
        services
            .AddOptions<TemplateClientOptions>()
            .BindConfiguration(TemplateClientOptions.OptionsKey)
            .Configure(configureOptions ?? (_ => { }));

        services.AddTransient<ErrorDelegatingHandler>();

        services
            .AddHttpClient<ITemplateClient, TemplateClient>()
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        return services;
    }
}
