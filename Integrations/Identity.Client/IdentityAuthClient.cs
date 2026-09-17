using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SQLModule.Identity.Client;

internal sealed class IdentityAuthClient(
    HttpClient httpClient,
    IOptions<IdentityAuthClientOptions> options) : IIdentityAuthClient
{
    private const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IdentityAuthClientOptions options = options.Value;

    public async Task<IdentityLoginResult> LoginAndExchangeAsync(
        string email, string password, string audience, CancellationToken ct = default)
    {
        var loginBody = await LoginAsync(email, password, ct);
        if (loginBody is null)
        {
            return Unavailable();
        }

        if (loginBody.Value.Unauthorized)
        {
            return new IdentityLoginResult(IdentityLoginOutcome.InvalidCredentials, null, 0);
        }

        if (String.IsNullOrEmpty(loginBody.Value.AccessToken))
        {
            return Unavailable();
        }

        Console.WriteLine($"[DEBUG] subjectToken len={loginBody.Value.AccessToken?.Length}");
        var exchanged = await ExchangeAsync(loginBody.Value.AccessToken, audience, ct);
        if (exchanged is null || String.IsNullOrEmpty(exchanged.AccessToken))
        {
            return Unavailable();
        }

        return new IdentityLoginResult(IdentityLoginOutcome.Success, exchanged.AccessToken, exchanged.ExpiresIn);
    }

    private async Task<(bool Unauthorized, string? AccessToken)?> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(
                "api/v1/auth/login", new { email, password }, JsonOptions, ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return (true, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<LoginBody>(JsonOptions, ct);
        return (false, body?.AccessToken);
    }

    private async Task<ExchangeBody?> ExchangeAsync(string subjectToken, string audience, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/token/exchange")
        {
            Content = JsonContent.Create(
                new { grantType = GrantType, subjectToken, audience },
                options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}")));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return null;
        }

        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ExchangeBody>(JsonOptions, ct)
            : null;
    }

    private static IdentityLoginResult Unavailable() =>
        new(IdentityLoginOutcome.Unavailable, null, 0);

    private sealed record LoginBody(string AccessToken);
    private sealed record ExchangeBody(string AccessToken, int ExpiresIn);
}
