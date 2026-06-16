using Microsoft.Extensions.DependencyInjection;
using TemplateApi.Client.Configurations;
using TemplateApi.Client.Template;

namespace TemplateApi.Client;

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
