namespace SQLModule.Web.Common.Auth.Isolated;

/// <summary>Расширения для регистрации фейковой аутентификации в изолированном режиме.</summary>
public static class IsolatedAuthExtensions
{
    /// <summary>
    /// Добавляет фейковую аутентификацию Robot: все запросы автоматически аутентифицируются
    /// как robot@scoodle.local с ролями Teacher/Admin/Student.
    /// </summary>
    public static IServiceCollection AddFakeAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(RobotAuthOptions.RobotAuthenticationScheme)
            .AddScheme<RobotAuthOptions, RobotAuthenticationHandler>(
                RobotAuthOptions.RobotAuthenticationScheme,
                cfg => { cfg.ForwardDefaultSelector = _ => RobotAuthOptions.RobotAuthenticationScheme; });

        return services;
    }
}
