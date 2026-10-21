using SQLModule.Application.Seeding;
using SQLModule.Data.Core.Migrations;
using SQLModule.Sandbox;
using SQLModule.Sandbox.DbmsProbing;
using SQLModule.Web.Common;

namespace SQLModule.Host;

/// <summary>Регистрация сидеров и фейков изолированного режима, а также инициализация БД при старте.</summary>
internal static class SeedingExtensions
{
    /// <summary>Фейковая песочница (флаг UseFakeSandbox) и сидеры данных.</summary>
    public static IServiceCollection AddSeeding(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue<bool>(IsolatedKeys.UseFakeSandbox))
        {
            services.AddSingleton<ISandboxExecutor, FakeSandboxExecutor>();
            services.AddSingleton<IDbmsProbe, AlwaysOkProbe>();
        }

        if (configuration.GetValue<bool>(IsolatedKeys.SeedDemoData))
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
        return services;
    }

    /// <summary>Применяет миграции и независимо запускает включённые demo/smoke-сидеры.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
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
