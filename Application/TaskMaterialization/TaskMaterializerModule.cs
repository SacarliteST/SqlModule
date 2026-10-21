using Microsoft.Extensions.DependencyInjection;

namespace SQLModule.Application.TaskMaterialization;

public static class TaskMaterializerModule
{
    public static IServiceCollection AddTaskMaterializer(this IServiceCollection services)
    {
        services.AddScoped<ITaskMaterializer, TaskMaterializer>();
        services.AddScoped<ITargetDbReferentialIntegrityValidator, TargetDbReferentialIntegrityValidator>();
        services.AddScoped<ITargetDbDataValidator, TargetDbDataValidator>();
        return services;
    }
}
