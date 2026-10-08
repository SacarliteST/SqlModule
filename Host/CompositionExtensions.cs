using SQLModule.Application;
using SQLModule.Data;
using SQLModule.Education.Client;
using SQLModule.Identity.Client;
using SQLModule.Kafka.Publisher;
using SQLModule.Sandbox;
using SQLModule.Web;

namespace SQLModule.Host;

/// <summary>Composition root приложения: подключает слои и инфраструктурные реализации.</summary>
internal static class CompositionExtensions
{
    private const string ModuleIntegrationSection = "ModuleIntegration";

    /// <summary>Регистрирует все слои приложения. Web не знает, как создаются Data, Sandbox и клиенты.</summary>
    public static IServiceCollection AddApplicationComposition(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddApplication()
            .AddWeb(configuration)
            .AddData(configuration)
            .AddIdentityAuthClient(configuration)
            .AddPlatformIntegrations(configuration)
            .AddSandbox();

    /// <summary>Регистрирует клиентов Education и Kafka, когда включён platform-профиль и публикация событий.</summary>
    private static IServiceCollection AddPlatformIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(ModuleIntegrationSection);
        if (!section.GetValue<bool>("Enabled") || !section.GetValue("Kafka:PublisherEnabled", true))
        {
            return services;
        }

        return services
            .AddEducationCompletionClient(section)
            .AddPracticeEventPublisher(section.GetSection("Kafka"));
    }
}
