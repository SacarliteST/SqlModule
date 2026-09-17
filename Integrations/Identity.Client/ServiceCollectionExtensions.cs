using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SQLModule.Identity.Client;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует клиент standalone-логина. Базовый адрес IdentityService берётся из
    /// <c>Auth:Authority</c> — того же значения, по которому SqlModule сам валидирует JWT.
    /// </summary>
    public static IServiceCollection AddIdentityAuthClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityAuthClientOptions>()
            .Bind(configuration.GetSection(IdentityAuthClientOptions.SectionKey));

        var authority = configuration["Auth:Authority"] ?? String.Empty;
        services.AddHttpClient<IIdentityAuthClient, IdentityAuthClient>(client =>
        {
            client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
