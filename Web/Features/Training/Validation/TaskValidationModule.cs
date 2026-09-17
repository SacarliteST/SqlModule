using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.Validation;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training.Validation;
using SQLModule.SqlAnalysis;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.DbmsCatalog.ValidationCapabilities.GetValidationCapabilities;
using SQLModule.Web.Features.Training.Validation.GetTaskValidation;
using SQLModule.Web.Features.Training.Validation.PreviewTaskValidation;
using SQLModule.Web.Features.Training.Validation.PublishTaskValidation;
using SQLModule.Web.Features.Training.Validation.UpdateTaskValidation;

namespace SQLModule.Web.Features.Training.Validation;

internal static class TaskValidationModule
{
    internal static IServiceCollection AddTaskValidation(this IServiceCollection services)
    {
        services.AddOptions<TaskValidationOptions>()
            .BindConfiguration(TaskValidationOptions.SectionKey)
            .Validate(options => options.MaxAttemptsLimit > 0, "Лимит попыток должен быть больше нуля.")
            .ValidateOnStart();

        services.AddSingleton<ITaskValidationConfigurationValidator, TaskValidationConfigurationValidator>();
        services.AddScoped<ITaskValidationEvaluationService, TaskValidationEvaluationService>();
        services.AddScoped<ITaskValidationSnapshotFactory, TaskValidationSnapshotFactory>();
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
        services.AddScoped<IValidator<TaskValidationConfigurationRequest>, TaskValidationConfigurationRequestValidator>();
        services.AddScoped<IValidator<TaskValidationPreviewRequest>, TaskValidationPreviewRequestValidator>();
        services.AddScoped<IValidator<PublishTaskValidationRequest>, PublishTaskValidationRequestValidator>();

        return services.AddSqlAnalysis();
    }
}
