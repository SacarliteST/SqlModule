using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SQLModule.Sandbox.Dialects;

namespace SQLModule.Sandbox;

/// <summary>Регистрирует инфраструктуру песочницы в DI-контейнере.</summary>
public static class SandboxExtensions
{
    public static IServiceCollection AddSandbox(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<SandboxOptions>, SandboxOptionsValidator>();
        services.AddOptions<SandboxOptions>()
            .BindConfiguration(SandboxOptions.SectionKey)
            .ValidateOnStart();
        services.AddSingleton<ISqlDialect, PostgresDialect>();
        services.AddSingleton<ISqlDialect, MySqlDialect>();
        services.AddSingleton<SqlDialectFactory>();
        services.AddSingleton<ISqlDialectFactory>(sp => sp.GetRequiredService<SqlDialectFactory>());
        services.AddSingleton<ISqlSyntaxFactory>(sp => sp.GetRequiredService<SqlDialectFactory>());
        services.AddSingleton<ISandboxExecutor, TestcontainersSandboxExecutor>();
        services.AddSingleton<ISchemaSqlGenerator, SchemaSqlGenerator>();
        return services;
    }
}
