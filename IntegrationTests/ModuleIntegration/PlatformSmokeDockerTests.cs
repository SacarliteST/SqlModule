using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Host;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Common.Isolated;

namespace SQLModule.IntegrationTests.ModuleIntegration;

[Collection(DockerTestCollection.Name)]
public sealed class PlatformSmokeDockerTests(DockerTestApplication app)
{
    private const string ServiceKey = "dev-sql-module-service-key-change-me";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact(DisplayName = "Platform smoke проходит на реальном sandbox и идемпотентно сохраняет outbox")]
    [Trait("Category", DockerTestCollection.Category)]
    public async Task PlatformSmoke_RealSandbox_ReturnsCorrectResultAndCreatesSingleOutboxPair()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();

        using var catalogRequest = new HttpRequestMessage(
            HttpMethod.Get,
            ApiRoutes.ModuleIntegration.TasksCatalog);
        catalogRequest.Headers.Add("X-Service-Key", ServiceKey);
        using var catalogResponse = await client.SendAsync(catalogRequest);
        var catalog = await catalogResponse.Content
            .ReadFromJsonAsync<List<ModuleTaskCatalogItemResponse>>();
        catalogResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        catalog.ShouldNotBeNull().Single(value => value.Ref == SmokeDataSeeder.TaskId.ToString());

        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionKey = $"smoke-session-{sessionId:N}";
        using var pushRequest = new HttpRequestMessage(
            HttpMethod.Post,
            ApiRoutes.ModuleIntegration.Sessions)
        {
            Content = JsonContent.Create(new UpsertModuleSessionRequest(
                sessionId,
                sessionKey,
                userId,
                SmokeDataSeeder.TaskId.ToString(),
                "http://localhost:3000/learning/smoke",
                DateTimeOffset.UtcNow.AddMinutes(15)))
        };
        pushRequest.Headers.Add("X-Service-Key", ServiceKey);
        using var pushResponse = await client.SendAsync(pushRequest);
        pushResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var idempotencyKey = Guid.NewGuid().ToString("D");
        using var firstResponse = await client.SendAsync(CreateSubmitRequest(
            userId,
            sessionId,
            idempotencyKey));
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<SubmitAttemptResponse>(JsonOptions);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        firstResult.ShouldNotBeNull();
        firstResult.IsCorrect.ShouldBeTrue();
        firstResult.ActualColumns.ShouldBe(["id"]);
        firstResult.ActualRows.ShouldBe([["1"]]);

        using var replayResponse = await client.SendAsync(CreateSubmitRequest(
            userId,
            sessionId,
            idempotencyKey));
        var replayResult = await replayResponse.Content.ReadFromJsonAsync<SubmitAttemptResponse>(JsonOptions);
        replayResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        replayResult.ShouldNotBeNull();
        replayResult.AttemptId.ShouldBe(firstResult.AttemptId);

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == sessionId)).ShouldBe(1);
        var outbox = await db.PendingPublishes.AsNoTracking()
            .Where(value => value.SessionId == sessionId)
            .OrderBy(value => value.Kind)
            .ToListAsync();
        outbox.Count.ShouldBe(2);
        outbox.Count(value => value.Kind == PendingPublishKind.Event).ShouldBe(1);
        outbox.Count(value => value.Kind == PendingPublishKind.Grade).ShouldBe(1);

        using var eventJson = JsonDocument.Parse(
            outbox.Single(value => value.Kind == PendingPublishKind.Event).MessageJson);
        eventJson.RootElement.GetProperty("kind").GetString().ShouldBe("sql_submit");
        eventJson.RootElement.GetProperty("payload").GetProperty("isCorrect").GetBoolean()
            .ShouldBeTrue();
        using var gradeJson = JsonDocument.Parse(
            outbox.Single(value => value.Kind == PendingPublishKind.Grade).MessageJson);
        gradeJson.RootElement.GetProperty("grade").GetInt32().ShouldBe(100);
        (await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId))
            .Status.ShouldBe(ModuleSessionStatus.CompletionPending);
    }

    private WebApplicationFactory<IHostMarker> CreatePlatformApplication() =>
        app.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Platform");
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UseRobotAuth"] = "false",
                    ["UseFakeSandbox"] = "false",
                    ["SeedDemoData"] = "false",
                    ["SeedSmokeData"] = "true",
                    ["DevTools:Enabled"] = "false",
                    ["ModuleIntegration:Kafka:PublisherEnabled"] = "false"
                }));
        });

    private static HttpRequestMessage CreateSubmitRequest(
        Guid userId,
        Guid sessionId,
        string idempotencyKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(new SubmitAttemptRequest(
                SmokeDataSeeder.TaskId,
                SmokeDataSeeder.ReferenceSql))
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Roles", "Student");
        request.Headers.Add("X-Test-SessionId", sessionId.ToString());
        return request;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
