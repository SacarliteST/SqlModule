using FluentValidation;
using SQLModule.Data;
using SQLModule.Domain;
using SQLModule.Domain.Common;
using SQLModule.Host.Common;

namespace SQLModule.Host;

internal static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocumentation();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddData(builder.Configuration);
        services.AddEndpoints();
        services.AddValidatorsFromAssemblyContaining<IHostMarker>();
        services.AddCqrs();
        services.AddFeatures();
    }

    public static ILogger CreateLogger()
    {
        using var factory = LoggerFactory.Create(options => options
            .AddConsole()
            .SetMinimumLevel(LogLevel.Trace));

        return factory.CreateLogger<Program>();
    }

    public static void ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseApiExceptionHandler();
        app.MapEndpoints();
    }
}
