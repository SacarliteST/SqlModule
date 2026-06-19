using FluentValidation;
using Microsoft.OpenApi;
using SQLModule.Contracts;
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
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SQLModule API",
                Version = "v1",
                Description = "API SQL-тренажёра (модуль Scoodle)."
            });

            c.SupportNonNullableReferenceTypes();
            c.UseAllOfToExtendReferenceSchemas();

            foreach (var assembly in new[] { typeof(IHostMarker).Assembly, typeof(ApiRoutes).Assembly })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml))
                {
                    c.IncludeXmlComments(xml, includeControllerXmlComments: false);
                }
            }
        });

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
