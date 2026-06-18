using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SQLModule.Client.TargetDb;

namespace SQLModule.Client;

/// <summary>Методы регистрации типизированных HTTP-клиентов в DI.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует <see cref="ITargetDbClient"/> и базовую HTTP-инфраструктуру клиента.
    /// <see cref="SqlModuleClientOptions"/> привязываются из секции
    /// <see cref="SqlModuleClientOptions.SectionKey"/> переданной конфигурации.
    /// </summary>
    public static IServiceCollection AddSqlModuleClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IConfiguration>(configuration);

        services.AddOptions<SqlModuleClientOptions>()
            .BindConfiguration(SqlModuleClientOptions.SectionKey);

        services.AddTransient<ErrorDelegatingHandler>();

        services.AddHttpClient<ITargetDbClient, TargetDbClient>((sp, http) =>
                http.BaseAddress = sp.GetRequiredService<IOptions<SqlModuleClientOptions>>()
                    .Value.BaseAddress)
            .AddHttpMessageHandler<ErrorDelegatingHandler>();

        return services;
    }
}
