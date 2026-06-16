using Contracts;

namespace TemplateApi.Host.Common;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointTypes = typeof(EndpointExtensions).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            services.AddTransient(typeof(IEndpoint), type);
        }

        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Template.TemplateObjects)
                       .WithTags("Templates");

        using var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoints(group);
        }

        return app;
    }
}
