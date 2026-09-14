using Microsoft.Extensions.DependencyInjection;

namespace SQLModule.Web.Common.Sandbox;

internal static class TaskMaterializerModule
{
    internal static IServiceCollection AddTaskMaterializer(this IServiceCollection services)
    {
        services.AddScoped<ITaskMaterializer, TaskMaterializer>();
        services.AddScoped<ITargetDbReferentialIntegrityValidator, TargetDbReferentialIntegrityValidator>();
        services.AddScoped<ITargetDbDataValidator, TargetDbDataValidator>();
        return services;
    }
}
