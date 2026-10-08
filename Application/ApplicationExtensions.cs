using Microsoft.Extensions.DependencyInjection;
using SQLModule.Application.Behaviors;
using SQLModule.Application.Cqrs;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Application.Training.Validation;

namespace SQLModule.Application;

/// <summary>Точка подключения прикладного слоя: CQRS-ядро и обработчики сценариев.</summary>
public static class ApplicationExtensions
{
    /// <summary>Регистрирует <see cref="ISender"/>, pipeline-поведения и обработчики сценариев.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISender, Sender>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped<ISqlQueryValidationRunner, SqlQueryValidationRunner>();
        services.AddScoped<ITaskValidationSnapshotFactory, TaskValidationSnapshotFactory>();
        return services
            .AddTaskMaterializer()
            .AddSqlTasks();
    }
}
