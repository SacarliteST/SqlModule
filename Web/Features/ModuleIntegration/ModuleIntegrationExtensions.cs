using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Education.Client;
using SQLModule.Kafka.Publisher;

namespace SQLModule.Web.Features.ModuleIntegration;

internal static class ModuleIntegrationExtensions
{
    internal static IServiceCollection AddModuleIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(ModuleIntegrationOptions.SectionKey);
        services.AddScoped<IPlatformStudentScope, PlatformStudentScope>();
        services.AddOptions<ModuleIntegrationOptions>()
            .Bind(section)
            .Validate(
                options => !options.Enabled || !String.IsNullOrWhiteSpace(options.ServiceKey),
                "ModuleIntegration:ServiceKey обязателен в platform-профиле.")
            .Validate(
                options => !options.Enabled || Uri.TryCreate(
                    options.EducationBaseUrl,
                    UriKind.Absolute,
                    out var uri) &&
                           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "ModuleIntegration:EducationBaseUrl должен быть абсолютным HTTP(S)-адресом в platform-профиле.")
            .Validate(
                options => !options.Enabled || !String.IsNullOrWhiteSpace(options.Kafka.BootstrapServers),
                "ModuleIntegration:Kafka:BootstrapServers обязателен в platform-профиле.")
            .Validate(
                options => !options.Enabled || !String.IsNullOrWhiteSpace(options.Kafka.EventsTopic),
                "ModuleIntegration:Kafka:EventsTopic обязателен в platform-профиле.")
            .Validate(
                options => !options.Enabled || options.Kafka is
                {
                    MaxAttempts: > 0,
                    InitialRetryDelaySeconds: > 0,
                    MaxRetryDelaySeconds: > 0
                },
                "Числовые настройки ModuleIntegration:Kafka должны быть больше нуля.")
            .Validate(
                options => !options.Enabled ||
                           options.Kafka.MaxRetryDelaySeconds >=
                           options.Kafka.InitialRetryDelaySeconds,
                "ModuleIntegration:Kafka:MaxRetryDelaySeconds не может быть меньше InitialRetryDelaySeconds.")
            .Validate(
                options => !options.Enabled || options.Publisher is
                {
                    PollIntervalSeconds: > 0,
                    BatchSize: > 0,
                    SentRetentionDays: > 0
                },
                "Числовые настройки ModuleIntegration:Publisher должны быть больше нуля.")
            .Validate(
                options => !options.Enabled || options.EducationCompletion is
                {
                    MaxAttempts: > 0,
                    InitialRetryDelaySeconds: > 0,
                    MaxRetryDelaySeconds: > 0,
                    RequestTimeoutSeconds: > 0
                },
                "Числовые настройки ModuleIntegration:EducationCompletion должны быть больше нуля.")
            .Validate(
                options => !options.Enabled ||
                           options.EducationCompletion.MaxRetryDelaySeconds >=
                           options.EducationCompletion.InitialRetryDelaySeconds,
                "ModuleIntegration:EducationCompletion:MaxRetryDelaySeconds не может быть меньше InitialRetryDelaySeconds.")
            .Validate(
                options => !options.Enabled || options.Lifecycle is
                {
                    CleanupIntervalMinutes: > 0,
                    RetentionDays: > 0,
                    BatchSize: > 0
                },
                "Числовые настройки ModuleIntegration:Lifecycle должны быть больше нуля.")
            .ValidateOnStart();

        if (section.GetValue<bool>(nameof(ModuleIntegrationOptions.Enabled)))
        {
            services.AddScoped<ModuleSessionCleanupProcessor>();
            services.AddHostedService<ModuleSessionCleanupWorker>();
        }

        if (section.GetValue<bool>(nameof(ModuleIntegrationOptions.Enabled)) &&
            section.GetValue("Kafka:PublisherEnabled", true))
        {
            services.AddEducationCompletionClient(section);
            services.AddPracticeEventPublisher(section.GetSection("Kafka"));
            services.AddScoped<PendingPublishProcessor>();
            services.AddHostedService<PendingPublishWorker>();
        }

        return services;
    }
}
