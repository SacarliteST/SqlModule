using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Training.Attempts;
﻿using System.Security.Claims;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Auth;
using SQLModule.Web.Common.Auth.Isolated;
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
        var useAllowAllCors = configuration.GetValue<bool>(IsolatedKeys.UseAllowAllCors);

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

        services.AddEndpointsApiExplorer();
        services.AddHealthChecks();
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddOpenApiDocumentation();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddEndpoints(configuration);
        services.AddValidatorsFromAssemblyContaining<IWebMarker>(includeInternalTypes: true);
        
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
}
