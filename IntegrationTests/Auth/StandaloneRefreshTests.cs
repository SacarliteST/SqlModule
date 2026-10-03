using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.Auth;
using SQLModule.Host;
using SQLModule.IntegrationTests.Infrastructure;

namespace SQLModule.IntegrationTests.Auth;

/// <summary>Standalone-вход: refresh-токен Identity доезжает до браузера, обновляется и отзывается через прокси SqlModule.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class StandaloneRefreshTests(TestApplication app)
{
    private sealed class FakeIdentityHandler : HttpMessageHandler
    {
        public List<string> Calls { get; } = [];
        public string? LastRevoked { get; private set; }
        public bool ExchangeFails { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            Calls.Add(path);
            var body = request.Content is null
                ? default
                : JsonDocument.Parse(await request.Content.ReadAsStringAsync(ct)).RootElement;

            switch (path)
            {
                case "/api/v1/auth/login":
                    return Json(new { accessToken = "identity-access-1", refreshToken = "refresh-1" });
                case "/api/v1/auth/refresh":
                    return body.GetProperty("refreshToken").GetString() == "refresh-1"
                        ? Json(new { accessToken = "identity-access-2", refreshToken = "refresh-2" })
                        : new HttpResponseMessage(HttpStatusCode.Unauthorized)
                        {
                            Content = JsonContent.Create(new { title = "Недействительный токен" }),
                        };
                case "/api/v1/auth/token/exchange":
                    return ExchangeFails
                        ? new HttpResponseMessage(HttpStatusCode.BadGateway)
                        : Json(new
                        {
                            accessToken = "exchanged-for-" + body.GetProperty("subjectToken").GetString(),
                            expiresIn = 1800,
                        });
                case "/api/v1/auth/logout":
                    LastRevoked = body.GetProperty("refreshToken").GetString();
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                default:
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value),
        };
    }

    private (WebApplicationFactory<IHostMarker> Factory, FakeIdentityHandler Identity) CreateApplication()
    {
        var identity = new FakeIdentityHandler();
        var factory = app.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddHttpClient("IIdentityAuthClient").ConfigurePrimaryHttpMessageHandler(() => identity)));
        return (factory, identity);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string route, object body) =>
        client.PostAsJsonAsync(route, body);

    [Fact(DisplayName = "Вход возвращает обменянный токен и refresh-токен Identity")]
    public async Task Login_ReturnsExchangedAccessTokenAndRefreshToken()
    {
        var (factory, _) = CreateApplication();
        using var client = factory.CreateClient();

        var response = await Post(client, ApiRoutes.Auth.Login, new StandaloneLoginRequest("t@e.edu", "pw"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<StandaloneLoginResponse>())!;
        body.AccessToken.ShouldBe("exchanged-for-identity-access-1");
        body.RefreshToken.ShouldBe("refresh-1");
        body.ExpiresIn.ShouldBe(1800);
    }

    [Fact(DisplayName = "Обновление ротирует refresh-токен и выдаёт новый обменянный токен")]
    public async Task Refresh_RotatesRefreshTokenAndExchangesNewAccessToken()
    {
        var (factory, identity) = CreateApplication();
        using var client = factory.CreateClient();

        var response = await Post(client, ApiRoutes.Auth.Refresh, new StandaloneRefreshRequest("refresh-1"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<StandaloneLoginResponse>())!;
        body.AccessToken.ShouldBe("exchanged-for-identity-access-2");
        body.RefreshToken.ShouldBe("refresh-2");
        identity.Calls.ShouldBe(["/api/v1/auth/refresh", "/api/v1/auth/token/exchange"]);
    }

    [Fact(DisplayName = "Недействительный refresh-токен → 401 без обмена")]
    public async Task Refresh_RejectedByIdentity_Returns401()
    {
        var (factory, identity) = CreateApplication();
        using var client = factory.CreateClient();

        var response = await Post(client, ApiRoutes.Auth.Refresh, new StandaloneRefreshRequest("stale"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        identity.Calls.ShouldBe(["/api/v1/auth/refresh"]);
    }

    [Fact(DisplayName = "Сбой обмена после ротации → 503 (клиент войдёт заново)")]
    public async Task Refresh_ExchangeFailure_Returns503()
    {
        var (factory, identity) = CreateApplication();
        identity.ExchangeFails = true;
        using var client = factory.CreateClient();

        var response = await Post(client, ApiRoutes.Auth.Refresh, new StandaloneRefreshRequest("refresh-1"));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact(DisplayName = "Пустой refresh-токен → 422")]
    public async Task Refresh_EmptyToken_Returns422()
    {
        var (factory, identity) = CreateApplication();
        using var client = factory.CreateClient();

        var response = await Post(client, ApiRoutes.Auth.Refresh, new StandaloneRefreshRequest(""));

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        identity.Calls.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Выход отзывает refresh-токен в Identity и отвечает 204")]
    public async Task Logout_RevokesRefreshToken()
    {
        var (factory, identity) = CreateApplication();
        using var client = factory.CreateClient();

        var response = await Post(client, ApiRoutes.Auth.Logout, new StandaloneRefreshRequest("refresh-2"));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        identity.LastRevoked.ShouldBe("refresh-2");
    }
}
