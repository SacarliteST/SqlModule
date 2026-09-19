using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SQLModule.Data;
using SQLModule.Data.Core.Migrations;
using SQLModule.Domain.Common;
using SQLModule.Identity.Client;
using SQLModule.Sandbox;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Auth;
using SQLModule.Web.Common.Auth.Isolated;
using SQLModule.Web.Common.Isolated;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;
using SQLModule.Web.Features.ModuleIntegration;
using SQLModule.Web.Features.Training.Attempts;

namespace SQLModule.Web;

/// <summary>Точки подключения Web-слоя к хост-приложению.</summary>
public static class WebExtensions
{
    private const string AllowAllCorsPolicy = "AllowAll";

    /// <summary>Регистрирует все сервисы Web-слоя.</summary>
    public static IServiceCollection AddWeb(this IServiceCollection services, IConfiguration configuration)
    {
        var useRobotAuth = configuration.GetValue<bool>(IsolatedKeys.UseRobotAuth);
        var useFakeSandbox = configuration.GetValue<bool>(IsolatedKeys.UseFakeSandbox);
        var useAllowAllCors = configuration.GetValue<bool>(IsolatedKeys.UseAllowAllCors);
        var seedDemoData = configuration.GetValue<bool>(IsolatedKeys.SeedDemoData);

        if (useRobotAuth)
        {
            services.AddFakeAuthentication();
        }
        else
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
        }

        services.AddAuthorization(o =>
        {
            o.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            o.AddPolicy(Policies.Admin, p => p
                .RequireRole(Roles.Admin)
                .AddRequirements(new NoPlatformSessionRequirement()));
            o.AddPolicy(Policies.ContentAuthor, p => p
                .RequireRole(Roles.Teacher, Roles.Admin)
                .AddRequirements(new NoPlatformSessionRequirement()));
            o.AddPolicy(Policies.Student, p => p.RequireRole(Roles.Student));
        });
        services.AddSingleton<IAuthorizationHandler, NoPlatformSessionHandler>();

        if (useAllowAllCors)
        {
            services.AddCors(options =>
                options.AddPolicy(AllowAllCorsPolicy, builder =>
                    builder.SetIsOriginAllowed(_ => true)
                           .AllowCredentials()
                           .AllowAnyMethod()
                           .AllowAnyHeader()));
        }

        if (useFakeSandbox)
        {
            services.AddSingleton<ISandboxExecutor, FakeSandboxExecutor>();
            services.AddSingleton<IDbmsProbe, AlwaysOkProbe>();
        }

        if (seedDemoData)
        {
            services.AddScoped<DemoDataSeeder>();
        }

        services.AddScoped<SmokeDataSeeder>();
        services.AddOptions<CourseSeedOptions>()
            .Bind(configuration.GetSection(CourseSeedOptions.SectionKey))
            .Validate(
                value => !value.Enabled || !String.IsNullOrWhiteSpace(value.FilePath),
                "CourseSeed:FilePath обязателен при CourseSeed:Enabled=true.")
            .ValidateOnStart();
        services.AddScoped<CourseDataSeeder>();

        services.AddEndpointsApiExplorer();
        services.AddHealthChecks()
            .AddCheck<SandboxPoolHealthCheck>("sandbox_pool");
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddOpenApiDocumentation();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.Configure<AttemptResultSnapshotsOptions>(
            configuration.GetSection(AttemptResultSnapshotsOptions.SectionKey));
        services.AddData(configuration);
        services.AddModuleIntegration(configuration);
        services.AddIdentityAuthClient(configuration);
        services.AddEndpoints(configuration);
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

        if (app.Configuration.GetValue<bool>(IsolatedKeys.UseAllowAllCors))
        {
            app.UseCors(AllowAllCorsPolicy);
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.MapEndpoints();
        return app;
    }

    /// <summary>Применяет миграции и независимо запускает включённые demo/smoke-сидеры.</summary>
    public static async Task InitializeWebAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>(IsolatedKeys.SkipDatabaseInitialization))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<IMigrationManager>().MigrateAsync();

        if (app.Configuration.GetValue<bool>(IsolatedKeys.SeedDemoData))
        {
            await sp.GetRequiredService<DemoDataSeeder>().SeedAsync();
        }

        if (app.Configuration.GetValue<bool>(IsolatedKeys.SeedSmokeData))
        {
            await sp.GetRequiredService<SmokeDataSeeder>().SeedAsync();
        }

        await sp.GetRequiredService<CourseDataSeeder>().SeedAsync();
    }
}
