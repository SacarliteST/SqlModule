using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Training.Validation;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.Validation;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training.Validation;
using SQLModule.SqlAnalysis;
using SQLModule.Application.Cqrs;
using SQLModule.Application.DbmsCatalog.ValidationCapabilities.GetValidationCapabilities;
using SQLModule.Application.Training.Validation.GetTaskValidation;
using SQLModule.Application.Training.Validation.PreviewTaskValidation;
using SQLModule.Application.Training.Validation.PublishTaskValidation;
using SQLModule.Application.Training.Validation.UpdateTaskValidation;

namespace SQLModule.Application.Training.Validation;

public static class TaskValidationModule
{
    public static IServiceCollection AddTaskValidation(this IServiceCollection services)
    {
        services.AddOptions<TaskValidationOptions>()
            .BindConfiguration(TaskValidationOptions.SectionKey)
            .Validate(options => options.MaxAttemptsLimit > 0, "Лимит попыток должен быть больше нуля.")
            .ValidateOnStart();

        services.AddSingleton<ITaskValidationConfigurationValidator, TaskValidationConfigurationValidator>();
        services.AddScoped<ITaskValidationEvaluationService, TaskValidationEvaluationService>();
        services.AddScoped<
            IRequestHandler<GetValidationCapabilitiesQuery, Result<DbmsValidationCapabilitiesResponse>>,
            GetValidationCapabilitiesHandler>();
        services.AddScoped<
            IRequestHandler<GetTaskValidationQuery, Result<TaskValidationConfigurationResponse>>,
            GetTaskValidationHandler>();
        services.AddScoped<
            IRequestHandler<UpdateTaskValidationCommand, Result<TaskValidationConfigurationResponse>>,
            UpdateTaskValidationHandler>();
        services.AddScoped<
            IRequestHandler<PreviewTaskValidationCommand, Result<TaskValidationPreviewResponse>>,
            PreviewTaskValidationHandler>();
        services.AddScoped<
            IRequestHandler<PublishTaskValidationCommand, Result<TaskValidationConfigurationResponse>>,
            PublishTaskValidationHandler>();

        return services.AddSqlAnalysis();
    }
}
