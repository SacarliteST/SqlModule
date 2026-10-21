using SQLModule.Application.Auth;
using SQLModule.Identity.Client;

namespace SQLModule.Host;

/// <summary>Адаптер порта standalone-входа к клиенту IdentityService.</summary>
internal sealed class IdentityStandaloneAuthGateway(IIdentityAuthClient client) : IStandaloneAuthGateway
{
    public async Task<StandaloneAuthResult> LoginAsync(
        string email, string password, string audience, CancellationToken ct) =>
        Map(await client.LoginAndExchangeAsync(email, password, audience, ct));

    public async Task<StandaloneAuthResult> RefreshAsync(string refreshToken, string audience, CancellationToken ct) =>
        Map(await client.RefreshAndExchangeAsync(refreshToken, audience, ct));

    public Task LogoutAsync(string refreshToken, CancellationToken ct) => client.LogoutAsync(refreshToken, ct);

    private static StandaloneAuthResult Map(IdentityLoginResult result) => new(
        result.Outcome switch
        {
            IdentityLoginOutcome.Success => StandaloneAuthOutcome.Success,
            IdentityLoginOutcome.InvalidCredentials => StandaloneAuthOutcome.InvalidCredentials,
            _ => StandaloneAuthOutcome.Unavailable,
        },
        result.AccessToken,
        result.ExpiresIn,
        result.RefreshToken,
        result.ErrorTitle,
        result.ErrorDetail);
}
