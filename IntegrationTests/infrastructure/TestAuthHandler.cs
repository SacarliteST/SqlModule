using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>
/// Заглушка аутентификации для интеграционных тестов.
/// Читает X-Test-UserId и X-Test-Roles из заголовков запроса и строит ClaimsPrincipal без обращения к Identity.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";
    private const string UserIdHeader = "X-Test-UserId";
    private const string RolesHeader = "X-Test-Roles";
    private const string DefaultRole = "Teacher";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers[UserIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
        var rolesHeader = Request.Headers[RolesHeader].FirstOrDefault() ?? DefaultRole;

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        claims.AddRange(
            rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Select(r => new Claim(ClaimTypes.Role, r)));

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)),
            SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
