using Microsoft.Extensions.DependencyInjection;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.Sandbox;

/// <summary>Регистрирует инфраструктуру песочницы в DI-контейнере.</summary>
public static class SandboxExtensions
{
    public static IServiceCollection AddSandbox(this IServiceCollection services)
    {
        services.AddOptions<SandboxOptions>().BindConfiguration(SandboxOptions.SectionKey);
        services.AddSingleton<ISqlDialect, PostgresDialect>();
        services.AddSingleton<ISqlDialect, MySqlDialect>();
        services.AddSingleton<ISqlDialectFactory, SqlDialectFactory>();
        services.AddSingleton<ISandboxExecutor, TestcontainersSandboxExecutor>();
        return services;
    }
}
