namespace SQLModule.Host;

internal static class SandboxHealthExtensions
{
    /// <summary>Добавляет проверку доступности sandbox-пула в общий health endpoint.</summary>
    public static IServiceCollection AddSandboxHealthCheck(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<SandboxPoolHealthCheck>("sandbox_pool");
        return services;
    }
}
