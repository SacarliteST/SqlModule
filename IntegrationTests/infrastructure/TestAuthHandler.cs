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
    private const string DisplayNameHeader = "X-Test-DisplayName";
    private const string AnonymousHeader = "X-Test-Anonymous";
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
        if (Request.Headers[AnonymousHeader].FirstOrDefault() == "true")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers[UserIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();
        var rolesHeader = Request.Headers[RolesHeader].FirstOrDefault() ?? DefaultRole;
        var displayName = Request.Headers[DisplayNameHeader].FirstOrDefault() ?? "Test User";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName)
        };
        claims.AddRange(
            rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Select(r => new Claim(ClaimTypes.Role, r)));

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)),
            SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
