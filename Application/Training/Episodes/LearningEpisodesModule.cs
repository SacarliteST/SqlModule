using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.SpdIntegration;

namespace SQLModule.Application.Training.Episodes;

public static class LearningEpisodesModule
{
    public static IServiceCollection AddLearningEpisodes(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<LearningEpisodeOptions>()
            .Bind(configuration.GetSection(LearningEpisodeOptions.SectionKey))
            .Validate(options => options.CleanupIntervalSeconds > 0,
                "LearningEpisodes:CleanupIntervalSeconds должен быть больше нуля.")
            .Validate(options => options.BatchSize > 0,
                "LearningEpisodes:BatchSize должен быть больше нуля.")
            .ValidateOnStart();
        services.AddScoped<LearningEpisodeService>();
        services.AddScoped<LearningEpisodeOutbox>();
        services.AddScoped<ILearningEpisodeService>(provider =>
            provider.GetRequiredService<LearningEpisodeService>());
        services.AddScoped<LearningEpisodeTimeoutProcessor>();
        services.AddHostedService<LearningEpisodeTimeoutWorker>();
        return services;
    }
}
