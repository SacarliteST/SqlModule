using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQLModule.Web.Common.Auth.Isolated;

/// <summary>
/// Обработчик аутентификации для изолированного режима.
/// Всегда возвращает Identity робота — без обращения к Identity-сервису.
/// </summary>
public sealed class RobotAuthenticationHandler(
    IOptionsMonitor<RobotAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<RobotAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(RobotIdentity.ClaimsIdentity),
            RobotAuthOptions.RobotAuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
