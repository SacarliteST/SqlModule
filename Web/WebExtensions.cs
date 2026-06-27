using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SQLModule.Data;
using SQLModule.Data.Core.Migrations;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;

namespace SQLModule.Web;

/// <summary>Точки подключения Web-слоя к хост-приложению.</summary>
public static class WebExtensions
{
    /// <summary>Регистрирует все сервисы Web-слоя.</summary>
    public static IServiceCollection AddWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocumentation();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddData(configuration);
        services.AddEndpoints();
        services.AddValidatorsFromAssemblyContaining<IWebMarker>(includeInternalTypes: true);
        services.AddCqrs();
        services.AddFeatures();
        return services;
    }

    /// <summary>Подключает middleware и маппинг маршрутов Web-слоя.</summary>
    public static WebApplication UseWeb(this WebApplication app)
    {
        app.UseApiExceptionHandler();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapEndpoints();
        return app;
    }

    /// <summary>Применяет EF Core миграции при старте.</summary>
    public static async Task InitializeWebAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMigrationManager>().MigrateAsync();
    }
}
