using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Cqrs;
using SQLModule.Application.SpdIntegration;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Application.Training.Skills;

public static class SkillsModule
{
    public static IServiceCollection AddSkills(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EducationSpdOptions>()
            .Bind(configuration.GetSection(EducationSpdOptions.SectionKey))
            .Validate(
                options => !options.Enabled || options.ModuleKey == "sql",
                "EducationSpd:ModuleKey для SQL-модуля должен быть равен 'sql'.")
            .Validate(
                options => !options.Enabled || !String.IsNullOrWhiteSpace(options.Kafka.SkillCatalogTopic),
                "EducationSpd:Kafka:SkillCatalogTopic обязателен при включённой интеграции.")
            .Validate(
                options => !options.Enabled || !String.IsNullOrWhiteSpace(options.Kafka.EpisodeFinalizedTopic),
                "EducationSpd:Kafka:EpisodeFinalizedTopic обязателен при включённой интеграции.")
            .ValidateOnStart();
        services.AddScoped<SkillCatalogOutbox>();
        services.AddScoped<IRequestHandler<CreateSkillCommand, Result<SkillResponse>>, CreateSkillHandler>();
        services.AddScoped<IRequestHandler<GetSkillByIdQuery, Result<SkillResponse>>, GetSkillByIdHandler>();
        services.AddScoped<IRequestHandler<GetSkillsQuery, Result<PageResponse<SkillResponse>>>, GetSkillsHandler>();
        services.AddScoped<IRequestHandler<UpdateSkillCommand, Result<SkillResponse>>, UpdateSkillHandler>();
        services.AddScoped<IRequestHandler<DeleteSkillCommand, Result>, DeleteSkillHandler>();
        services.AddScoped<IRequestHandler<RestoreSkillCommand, Result<SkillResponse>>, RestoreSkillHandler>();
        return services;
    }
}
