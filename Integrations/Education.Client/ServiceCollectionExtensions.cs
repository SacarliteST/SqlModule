using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SQLModule.PlatformIntegration.Abstractions;

namespace SQLModule.Education.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEducationCompletionClient(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        services.AddOptions<EducationClientOptions>().Bind(section);
        services.AddTransient<ServiceKeyDelegatingHandler>();
        services.AddHttpClient<IEducationCompletionClient, EducationCompletionClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<EducationClientOptions>>().Value;
            client.BaseAddress = new Uri(options.EducationBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.EducationCompletion.RequestTimeoutSeconds);
        }).AddHttpMessageHandler<ServiceKeyDelegatingHandler>();
        return services;
    }
}
