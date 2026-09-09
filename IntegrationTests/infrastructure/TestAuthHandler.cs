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
    private const string EmailHeader = "X-Test-Email";
    private const string AnonymousHeader = "X-Test-Anonymous";
    private const string SessionIdHeader = "X-Test-SessionId";
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
        var email = Request.Headers[EmailHeader].FirstOrDefault() ?? "test.user@example.com";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email)
        };
        claims.AddRange(
            rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Select(r => new Claim(ClaimTypes.Role, r)));
        var sessionId = Request.Headers[SessionIdHeader].FirstOrDefault();
        if (!String.IsNullOrWhiteSpace(sessionId))
        {
            claims.Add(new Claim("session_id", sessionId));
        }

        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)),
            SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
