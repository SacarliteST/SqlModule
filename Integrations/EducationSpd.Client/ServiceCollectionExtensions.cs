using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.EducationSpd.Abstractions;

namespace SQLModule.EducationSpd.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEducationSpdRecommendationClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(EducationSpdRecommendationClientOptions.SectionKey);
        services.AddOptions<EducationSpdRecommendationClientOptions>()
            .Bind(section)
            .Validate(options => !options.Enabled || Uri.TryCreate(
                options.BaseUrl,
                UriKind.Absolute,
                out var uri) && uri.Scheme is "http" or "https",
                "EducationSpd:Recommendations:BaseUrl должен быть абсолютным HTTP(S)-адресом.")
            .Validate(options => !options.Enabled || !String.IsNullOrWhiteSpace(options.ServiceKey),
                "EducationSpd:Recommendations:ServiceKey обязателен.")
            .Validate(options => options.RequestTimeoutSeconds > 0,
                "EducationSpd:Recommendations:RequestTimeoutSeconds должен быть больше нуля.")
            .ValidateOnStart();
        if (!section.GetValue<bool>(nameof(EducationSpdRecommendationClientOptions.Enabled)))
        {
            return services;
        }

        services.AddHttpClient<IEducationSpdRecommendationClient, EducationSpdRecommendationClient>();
        return services;
    }
}
