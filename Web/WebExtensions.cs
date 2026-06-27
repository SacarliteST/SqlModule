using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using SQLModule.Data;
using SQLModule.Data.Core.Migrations;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Auth;

namespace SQLModule.Web;

/// <summary>Точки подключения Web-слоя к хост-приложению.</summary>
public static class WebExtensions
{
    /// <summary>Регистрирует все сервисы Web-слоя.</summary>
    public static IServiceCollection AddWeb(this IServiceCollection services, IConfiguration configuration)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionKey).Get<AuthOptions>()
                          ?? new AuthOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.Authority = authOptions.Authority;
                o.Audience = authOptions.Audience;
                o.TokenValidationParameters.NameClaimType = ClaimTypes.NameIdentifier;
                o.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                o.RequireHttpsMetadata = false;
            });

        services.AddAuthorization(o =>
        {
            o.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            o.AddPolicy(Policies.Admin, p => p.RequireRole(Roles.Admin));
            o.AddPolicy(Policies.ContentAuthor, p => p.RequireRole(Roles.Teacher, Roles.Admin));
            o.AddPolicy(Policies.Student, p => p.RequireRole(Roles.Student));
        });

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

        app.UseAuthentication();
        app.UseAuthorization();
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
