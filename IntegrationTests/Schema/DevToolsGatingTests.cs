using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Host;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Sandbox;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;
using Testcontainers.PostgreSql;

namespace SQLModule.IntegrationTests.Schema;

/// <summary>
/// Тест гейтинга dev-эндпоинтов: при DevTools:Enabled=false структурные мутации схемы (POST/PUT/DELETE
/// мета-таблиц) возвращают 404; CreateSchema и SubmitAttempt доступны всегда.
/// </summary>
public sealed class DevToolsGatingTests : IClassFixture<DevToolsGatingTests.DevDisabledApp>
{
    private readonly HttpClient client;

    public DevToolsGatingTests(DevDisabledApp app)
    {
        client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
    }

    [Fact(DisplayName = "E6: DevTools disabled → POST meta-tables возвращает 404 или 405 (эндпоинт не зарегистрирован)")]
    public async Task DevEndpoint_DevToolsDisabled_ReturnsNotRegistered()
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Schema.MetaTables.Collection, new { });
        // 404 — если URL не существует совсем; 405 — если URL есть для другого метода (GET/list остаётся),
        // но POST (dev-only create) не зарегистрирован. Оба варианта означают «dev-эндпоинт недоступен».
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact(DisplayName = "E6: DevTools disabled → POST schemas НЕ 404 (роут зарегистрирован)")]
    public async Task CreateSchema_DevToolsDisabled_IsRegistered()
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Schema.SchemaBuilder.Collection, new { });
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "E6: DevTools disabled → POST attempts НЕ 404 (роут зарегистрирован)")]
    public async Task SubmitAttempt_DevToolsDisabled_IsRegistered()
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Training.Attempts.Collection, new { });
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "E6: DevTools disabled → GET meta-tables НЕ 404 (чтение доступно)")]
    public async Task GetMetaTables_DevToolsDisabled_IsRegistered()
    {
        var response = await client.GetAsync(ApiRoutes.Schema.MetaTables.ForPagination(0, 10));
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }

    public sealed class DevDisabledApp : WebApplicationFactory<IHostMarker>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
            .WithImage(DockerImages.PostgreSql)
            .WithDatabase("devtools_gating_test")
            .WithUsername("testUser")
            .WithPassword("testPass")
            .WithName($"devtools_gating_{Guid.NewGuid():N}")
            .WithCleanUp(true)
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");

            builder.ConfigureAppConfiguration(config =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { ConfigConstants.DbConnection, postgres.GetConnectionString() },
                    { "DevTools:Enabled", "false" }
                }));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IDbmsProbe, AlwaysOkProbe>();
                services.AddSingleton<ISandboxExecutor, FakeSandboxExecutor>();

                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services.PostConfigure<AuthenticationOptions>(o =>
                {
                    o.DefaultScheme = TestAuthHandler.SchemeName;
                    o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                });
            });
        }

        public Task InitializeAsync() => postgres.StartAsync();

        Task IAsyncLifetime.DisposeAsync() => postgres.StopAsync();
    }
}
