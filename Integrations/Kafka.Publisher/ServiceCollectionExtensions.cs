using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.PlatformIntegration.Abstractions;

namespace SQLModule.Kafka.Publisher;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPracticeEventPublisher(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        services.AddOptions<KafkaPublisherOptions>().Bind(section);
        services.AddSingleton<IPracticeEventPublisher, KafkaPracticeEventPublisher>();
        return services;
    }
}
