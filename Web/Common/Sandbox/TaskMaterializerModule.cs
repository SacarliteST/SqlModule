using Microsoft.Extensions.DependencyInjection;

namespace SQLModule.Web.Common.Sandbox;

internal static class TaskMaterializerModule
{
    internal static IServiceCollection AddTaskMaterializer(this IServiceCollection services)
    {
        services.AddScoped<ITaskMaterializer, TaskMaterializer>();
        return services;
    }
}
