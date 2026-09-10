using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Host;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Common.Auth;

namespace SQLModule.IntegrationTests.ModuleIntegration;

[Collection(IntegrationTestCollection.Name)]
public sealed class TeacherAuthoringAuthenticationTests(TestApplication app)
{
    private const string Issuer = "https://identity.test";
    private const string PlatformAudience = "sql-module-api";
    private static readonly SymmetricSecurityKey SigningKey = new(
        Encoding.UTF8.GetBytes("teacher-authoring-integration-test-key-2026"));
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact(DisplayName = "TAI-009: platform Teacher JWT без session_id читает темы и создаёт SQL-задание")]
    public async Task PlatformTeacherToken_WithoutSession_CanUseAuthoringApi()
    {
        var userId = Guid.NewGuid();
        var fixture = await SeedAuthoringFixtureAsync();
        using var application = CreateJwtApplication("Platform", PlatformAudience);
        using var client = CreateBearerClient(application, CreateToken(
            PlatformAudience,
            Roles.Teacher,
            userId));

        using var topicsResponse = await client.GetAsync(
            $"{ApiRoutes.Training.Topics.Collection}?offset=0&limit=20");
        topicsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var request = new CreateSqlTaskRequest(
            fixture.TopicId,
            $"Authoring handoff {Guid.NewGuid():N}",
            "Создано с помощью teacher authoring JWT.",
            2,
            new ReferenceQueryRequest(fixture.TargetDbId, "SELECT 1", false, false));
        using var createResponse = await client.PostAsJsonAsync(
            ApiRoutes.Training.SqlTasks.Collection,
            request);

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var task = await createResponse.Content.ReadFromJsonAsync<SqlTaskResponse>(JsonOptions);
        task.ShouldNotBeNull();
        task.CreatedById.ShouldBe(userId);
        task.PublicationStatus.ShouldBe(PublicationStatus.Draft);
        task.SqlQueryId.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "TAI-009: platform Student JWT не получает доступ к authoring API")]
    public async Task PlatformStudentToken_CannotUseAuthoringApi()
    {
        using var application = CreateJwtApplication("Platform", PlatformAudience);
        using var client = CreateBearerClient(application, CreateToken(
            PlatformAudience,
            Roles.Student,
            Guid.NewGuid()));

        using var readResponse = await client.GetAsync(
            $"{ApiRoutes.Training.Topics.Collection}?offset=0&limit=20");
        using var createResponse = await client.PostAsJsonAsync(
            ApiRoutes.Training.Topics.Collection,
            new Contracts.Training.Topic.CreateTopicRequest("Forbidden", null));

        readResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private async Task<(Guid TopicId, Guid TargetDbId)> SeedAuthoringFixtureAsync()
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DbmsDictionary.Create(
            $"Authoring {Guid.NewGuid():N}",
            "postgres",
            "postgres:latest",
            5432,
            "POSTGRES_USER",
            "POSTGRES_PASSWORD",
            "POSTGRES_DB",
            null,
            "authoring",
            "authoring",
            "authoring");
        var targetDb = TargetDb.Create(
            dbms.Id,
            $"Authoring_{Guid.NewGuid():N}",
            null,
            false);
        var topic = Topic.Create($"Authoring {Guid.NewGuid():N}");
        db.AddRange(dbms, targetDb, topic);
        await db.SaveChangesAsync();
        return (topic.Id, targetDb.Id);
    }

    private WebApplicationFactory<IHostMarker> CreateJwtApplication(
        string environment,
        string audience) => app.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment(environment);
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Authority"] = Issuer,
                ["Auth:Audience"] = audience,
                ["ModuleIntegration:Kafka:PublisherEnabled"] = "false"
            }));
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey,
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });
        });
    });

    private static HttpClient CreateBearerClient(
        WebApplicationFactory<IHostMarker> application,
        string token)
    {
        var client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateToken(
        string audience,
        string role,
        Guid userId,
        Guid? sessionId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "Integration User"),
            new(ClaimTypes.Role, role)
        };
        if (sessionId.HasValue)
        {
            claims.Add(new Claim("session_id", sessionId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            Issuer,
            audience,
            claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
