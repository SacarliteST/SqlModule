using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SQLModule.Sandbox.Dialects;
using SQLModule.Sandbox.Pooling;

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
        services.AddSingleton<TestcontainersSandboxExecutor>();
        services.AddSingleton<PooledSandboxExecutor>();
        services.AddSingleton<ISandboxExecutor>(provider =>
            provider.GetRequiredService<IOptions<SandboxOptions>>().Value.Pool.Enabled
                ? provider.GetRequiredService<PooledSandboxExecutor>()
                : provider.GetRequiredService<TestcontainersSandboxExecutor>());
        services.TryAddSingleton<ISandboxPoolProfileSource, EmptySandboxPoolProfileSource>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SandboxPoolHealthMonitor>();
        services.AddSingleton<ISandboxPoolHealthMonitor>(sp =>
            sp.GetRequiredService<SandboxPoolHealthMonitor>());
        services.AddSingleton<SandboxPoolInstance>();
        services.AddSingleton<ISandboxWorkerFactory, TestcontainersSandboxWorkerFactory>();
        services.AddSingleton(sp => new LocalSandboxLeaseManager(
            sp.GetRequiredService<ISandboxWorkerFactory>(),
            sp.GetRequiredService<IOptions<SandboxOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalSandboxLeaseManager>>(),
            healthMonitor: sp.GetRequiredService<SandboxPoolHealthMonitor>(),
            timeProvider: sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<ISandboxLeaseManager>(sp => sp.GetRequiredService<LocalSandboxLeaseManager>());
        services.AddSingleton<ISandboxPoolLifecycle>(sp => sp.GetRequiredService<LocalSandboxLeaseManager>());
        services.AddSingleton<ISandboxIsolationManager, SandboxIsolationManager>();
        services.AddHostedService<SandboxPoolHostedService>();
        services.AddSingleton<ISchemaSqlGenerator, SchemaSqlGenerator>();
        return services;
    }
}
