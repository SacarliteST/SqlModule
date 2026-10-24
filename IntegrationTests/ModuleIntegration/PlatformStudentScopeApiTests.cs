using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Host;
using SQLModule.IntegrationTests.Infrastructure;
using SQLModule.Sandbox;
using SQLModule.Application.Seeding;
using SQLModule.Sandbox.DbmsProbing;

namespace SQLModule.IntegrationTests.ModuleIntegration;

/// <summary>Сценарии ТЗ «платформенная сессия ограничена одним заданием».</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class PlatformStudentScopeApiTests(TestApplication app)
{
    private WebApplicationFactory<IHostMarker> CreatePlatformApplication() =>
        app.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Platform");
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ModuleIntegration:Enabled"] = "true",
                    ["ModuleIntegration:ServiceKey"] = "platform-integration-test-key",
                    ["ModuleIntegration:EducationBaseUrl"] = "http://education.test",
                    ["ModuleIntegration:Kafka:BootstrapServers"] = "kafka.test:9092",
                    ["ModuleIntegration:Kafka:EventsTopic"] = "scoodle.practice.events",
                    ["ModuleIntegration:Kafka:PublisherEnabled"] = "false"
                }));
        });

    private sealed record Scenario(
        WebApplicationFactory<IHostMarker> Platform,
        HttpClient Client,
        Guid UserId,
        Guid SessionId,
        Guid TaskId,
        Guid OtherTaskId,
        FakeSandboxExecutor Executor);

    private static async Task<Scenario> SeedAsync(
        WebApplicationFactory<IHostMarker> platform,
        DateTimeOffset? expiresAt,
        int? maxAttempts = null,
        Action<ModuleSession>? mutateSession = null)
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(platform);
        var otherTaskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(platform);
        using (var scope = platform.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ModuleSessions.Add(ModuleSession.Create(
                sessionId,
                "current-session-key",
                userId,
                taskId.ToString(),
                "https://platform.example/return",
                expiresAt,
                maxAttempts));
            await db.SaveChangesAsync();
        }
        if (mutateSession is not null)
        {
            using var scope = platform.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.ModuleSessions.SingleAsync(value => value.Id == sessionId);
            mutateSession(session);
            await db.SaveChangesAsync();
        }

        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();
        return new Scenario(platform, platform.CreateClient(), userId, sessionId, taskId, otherTaskId, executor);
    }

    private static HttpRequestMessage Request(
        HttpMethod method,
        string url,
        Guid userId,
        Guid? sessionId,
        string role = "Student",
        string? idempotencyKey = null)
    {
        var message = new HttpRequestMessage(method, url);
        message.Headers.Add("X-Test-UserId", userId.ToString());
        message.Headers.Add("X-Test-Roles", role);
        if (sessionId.HasValue)
        {
            message.Headers.Add("X-Test-SessionId", sessionId.Value.ToString());
        }

        if (idempotencyKey is not null)
        {
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return message;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        Scenario scenario,
        HttpMethod method,
        string url,
        string role = "Student",
        string? idempotencyKey = null)
    {
        using var request = Request(method, url, scenario.UserId, scenario.SessionId, role, idempotencyKey);
        return await scenario.Client.SendAsync(request);
    }

    private static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        response.StatusCode.ShouldBe(status, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadAsStringAsync()).ShouldContain(code);
    }

    private static async Task ShouldHaveNoSideEffectsAsync(Scenario scenario)
    {
        scenario.Executor.RunCallCount.ShouldBe(0);
        using var scope = scenario.Platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.UserId == scenario.UserId)).ShouldBe(0);
        (await db.AttemptReservations.CountAsync(value => value.Progress.UserId == scenario.UserId)).ShouldBe(0);
        (await db.PendingPublishes.CountAsync(value => value.SessionId == scenario.SessionId)).ShouldBe(0);
    }

    private static async Task<HttpResponseMessage> SubmitAsync(
        Scenario scenario, Guid taskId, string? idempotencyKey = null)
    {
        using var request = PlatformProfileAndCatalogTests.CreateSubmitRequest(
            taskId, scenario.UserId, scenario.SessionId, idempotencyKey);
        return await scenario.Client.SendAsync(request);
    }

    private static void ReturnIncorrectResult(Scenario scenario) =>
        scenario.Executor.OverrideRun = SQLModule.Common.Results.Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, ["unexpected"], [], 0, 1));

    [Fact(DisplayName = "Platform scope: своё задание и его схема доступны")]
    public async Task OwnTask_DetailsAndSchema_AreAvailable()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));

        using var details = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(scenario.TaskId));
        using var schema = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.Training.Student.ForTaskSchema(scenario.TaskId));

        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        schema.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Platform scope: чужое задание и его схема — 404 PlatformSession.TaskNotFound")]
    public async Task OtherTask_DetailsAndSchema_AreNotFound()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));

        using var details = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(scenario.OtherTaskId));
        using var schema = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.Training.Student.ForTaskSchema(scenario.OtherTaskId));

        await ShouldBeProblemAsync(details, HttpStatusCode.NotFound, "PlatformSession.TaskNotFound");
        await ShouldBeProblemAsync(schema, HttpStatusCode.NotFound, "PlatformSession.TaskNotFound");
    }

    [Fact(DisplayName = "Platform scope: схема учебной базы другого задания недоступна — teacher API закрыт для платформенного токена")]
    public async Task TargetDbSchema_IsNotReachableWithPlatformToken()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        Guid otherTargetDbId;
        using (var scope = platform.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            otherTargetDbId = await db.SqlTasks.Where(value => value.Id == scenario.OtherTaskId)
                .Select(value => value.SqlQuery.TargetDbId).SingleAsync();
        }

        var url = ApiRoutes.Schema.TargetDbs.ForSchema(otherTargetDbId);
        using var asStudent = await SendAsync(scenario, HttpMethod.Get, url);
        using var asTeacherWithSession = await SendAsync(scenario, HttpMethod.Get, url, role: "Teacher");

        asStudent.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        asTeacherWithSession.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "Platform scope: попытка по своему заданию — 201")]
    public async Task Submit_OwnTask_IsCreated()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));

        using var response = await SubmitAsync(scenario, scenario.TaskId);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        scenario.Executor.RunCallCount.ShouldBe(1);
    }

    [Fact(DisplayName = "Platform scope: попытка по чужому заданию — 404 до sandbox, без попытки, резервации и события")]
    public async Task Submit_OtherTask_IsRejectedBeforeAnySideEffect()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));

        using var response = await SubmitAsync(scenario, scenario.OtherTaskId);

        await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "PlatformSession.TaskNotFound");
        await ShouldHaveNoSideEffectsAsync(scenario);
    }

    [Theory(DisplayName = "Platform scope: каталог, темы и история недоступны платформенному токену — 403")]
    [InlineData("topics")]
    [InlineData("tasks")]
    [InlineData("attempts")]
    [InlineData("attempt-by-id")]
    public async Task CatalogTopicsAndHistory_AreForbidden(string endpoint)
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        var url = endpoint switch
        {
            "topics" => ApiRoutes.Training.Student.Topics,
            "tasks" => ApiRoutes.Training.Student.Tasks,
            "attempts" => ApiRoutes.Training.Student.Attempts,
            _ => ApiRoutes.Training.Student.ForAttempt(Guid.NewGuid())
        };

        using var response = await SendAsync(scenario, HttpMethod.Get, url);

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "PlatformSession.ScopeRestricted");
    }

    [Fact(DisplayName = "Platform scope: current-session доступен")]
    public async Task CurrentSession_IsAvailable()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));

        using var response = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.ModuleIntegration.CurrentSession);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain(scenario.TaskId.ToString());
    }

    [Fact(DisplayName = "Platform scope: finalize current-session идемпотентен по Idempotency-Key")]
    public async Task FinalizeCurrentSession_IsIdempotent()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        ReturnIncorrectResult(scenario);
        (await SubmitAsync(scenario, scenario.TaskId)).StatusCode.ShouldBe(HttpStatusCode.Created);
        var key = Guid.NewGuid().ToString("D");

        using var first = await SendAsync(
            scenario, HttpMethod.Post, ApiRoutes.ModuleIntegration.FinalizeCurrentSession, idempotencyKey: key);
        using var replay = await SendAsync(
            scenario, HttpMethod.Post, ApiRoutes.ModuleIntegration.FinalizeCurrentSession, idempotencyKey: key);

        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        replay.StatusCode.ShouldBe(HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        (await replay.Content.ReadAsStringAsync()).ShouldBe(await first.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Platform scope: истёкшая сессия не принимает попытку, но читается")]
    public async Task ExpiredSession_RejectsSubmit_ButRemainsReadable()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(-1));

        using var submit = await SubmitAsync(scenario, scenario.TaskId);
        using var current = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.ModuleIntegration.CurrentSession);
        using var details = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(scenario.TaskId));

        await ShouldBeProblemAsync(submit, HttpStatusCode.Conflict, "ModuleSession.Expired");
        current.StatusCode.ShouldBe(HttpStatusCode.OK);
        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        scenario.Executor.RunCallCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Platform scope: завершённая сессия не принимает попытку, но читается")]
    public async Task ClosedSession_RejectsSubmit_ButRemainsReadable()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(
            platform,
            DateTimeOffset.UtcNow.AddMinutes(10),
            mutateSession: session =>
            {
                session.MarkCompletionPending();
                session.MarkCompleted();
            });

        using var submit = await SubmitAsync(scenario, scenario.TaskId);
        using var current = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.ModuleIntegration.CurrentSession);
        using var details = await SendAsync(scenario, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(scenario.TaskId));

        await ShouldBeProblemAsync(submit, HttpStatusCode.Conflict, "ModuleSession.Closed");
        current.StatusCode.ShouldBe(HttpStatusCode.OK);
        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        await ShouldHaveNoSideEffectsAsync(scenario);
    }

    [Fact(DisplayName = "Platform scope: повтор submit с тем же ключом после завершения сессии возвращает исходную попытку")]
    public async Task SubmitReplay_AfterSessionCompletion_ReturnsOriginalAttempt()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        var key = Guid.NewGuid().ToString("D");

        using var first = await SubmitAsync(scenario, scenario.TaskId, key);
        using var replay = await SubmitAsync(scenario, scenario.TaskId, key);
        using var fresh = await SubmitAsync(scenario, scenario.TaskId);

        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        replay.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstAttempt = (await first.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("attemptId").GetGuid();
        (await replay.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("attemptId").GetGuid().ShouldBe(firstAttempt);
        fresh.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        scenario.Executor.RunCallCount.ShouldBe(1);
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == scenario.SessionId)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == scenario.SessionId && value.Kind == PendingPublishKind.Event)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == scenario.SessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(1);
    }

    [Fact(DisplayName = "Platform scope: лимит попыток закрывает сессию и отклоняет следующий submit до sandbox")]
    public async Task AttemptLimit_ClosesSession_AndRejectsNextSubmitBeforeSandbox()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10), maxAttempts: 2);
        ReturnIncorrectResult(scenario);

        using var first = await SubmitAsync(scenario, scenario.TaskId);
        using var second = await SubmitAsync(scenario, scenario.TaskId);
        using var third = await SubmitAsync(scenario, scenario.TaskId);

        first.StatusCode.ShouldBe(HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        second.StatusCode.ShouldBe(HttpStatusCode.Created, await second.Content.ReadAsStringAsync());
        await ShouldBeProblemAsync(third, HttpStatusCode.Conflict, "ModuleSession.AttemptsExhausted");
        scenario.Executor.RunCallCount.ShouldBe(2);

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempts = await db.Attempts.AsNoTracking()
            .Where(value => value.ModuleSessionId == scenario.SessionId)
            .OrderBy(value => value.AttemptNumber)
            .ToArrayAsync();
        attempts.Select(value => value.AttemptNumber).ShouldBe([1, 2]);
        var session = await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == scenario.SessionId);
        session.FinalizationReason.ShouldBe(Domain.Training.FinalizationReason.AttemptsExhausted);
        session.FinalScore.ShouldNotBeNull();
    }

    [Fact(DisplayName = "Platform scope: два параллельных submit при лимите 1 создают ровно одну попытку")]
    public async Task ParallelSubmits_WithOneAttemptLimit_CreateOneAttempt()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10), maxAttempts: 1);
        ReturnIncorrectResult(scenario);

        var responses = await Task.WhenAll(
            SubmitAsync(scenario, scenario.TaskId),
            SubmitAsync(scenario, scenario.TaskId));
        using var first = responses[0];
        using var second = responses[1];

        responses.Count(value => value.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(value => value.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == scenario.SessionId)).ShouldBe(1);
        (await db.Attempts.SingleAsync(value => value.ModuleSessionId == scenario.SessionId))
            .AttemptNumber.ShouldBe(1);
    }

    [Fact(DisplayName = "Platform scope: ошибка sandbox не расходует попытку")]
    public async Task SandboxFailure_DoesNotConsumeAttempt()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10), maxAttempts: 1);
        scenario.Executor.OverrideRun = SQLModule.Common.Results.Result<QueryResultSet>.Fail(
            SQLModule.Common.Results.Error.Unavailable("Sandbox.Unavailable", "test"));

        using var failed = await SubmitAsync(scenario, scenario.TaskId);
        scenario.Executor.OverrideRun = null;
        using var successful = await SubmitAsync(scenario, scenario.TaskId);

        failed.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        successful.StatusCode.ShouldBe(HttpStatusCode.Created, await successful.Content.ReadAsStringAsync());
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = await db.Attempts.SingleAsync(value => value.ModuleSessionId == scenario.SessionId);
        attempt.AttemptNumber.ShouldBe(1);
    }

    [Fact(DisplayName = "Standalone submit не создаёт платформенный outbox и не ограничивается лимитом")]
    public async Task StandaloneSubmit_HasNoSessionLimitOrPlatformOutbox()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10), maxAttempts: 1);
        var standaloneUser = Guid.NewGuid();

        for (var index = 0; index < 3; index++)
        {
            using var request = PlatformProfileAndCatalogTests.CreateSubmitRequest(
                scenario.OtherTaskId, standaloneUser, null);
            using var response = await scenario.Client.SendAsync(request);
            response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        }

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempts = await db.Attempts.AsNoTracking()
            .Where(value => value.UserId == standaloneUser && value.TaskId == scenario.OtherTaskId)
            .OrderBy(value => value.AttemptNumber)
            .ToArrayAsync();
        attempts.Select(value => value.AttemptNumber).ShouldBe([1, 2, 3]);
        attempts.All(value => value.ModuleSessionId == null).ShouldBeTrue();
        (await db.PendingPublishes.CountAsync(value => value.SessionId == scenario.SessionId)).ShouldBe(0);
    }

    [Fact(DisplayName = "Platform scope: null maxAttempts не ограничивает число отправок")]
    public async Task UnlimitedSession_AcceptsFiftyAttempts()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        ReturnIncorrectResult(scenario);

        for (var index = 0; index < 50; index++)
        {
            using var response = await SubmitAsync(scenario, scenario.TaskId);
            response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        }

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == scenario.SessionId)).ShouldBe(50);
        var session = await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == scenario.SessionId);
        session.Status.ShouldBe(ModuleSessionStatus.Active);
        session.FinalScore.ShouldBeNull();
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == scenario.SessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(0);
    }

    [Fact(DisplayName = "Platform scope: отправка после срока завершает сессию и создаёт один grade")]
    public async Task SubmitAfterExpiry_FinalizesSessionOnce()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        ReturnIncorrectResult(scenario);
        using (var first = await SubmitAsync(scenario, scenario.TaskId))
        {
            first.StatusCode.ShouldBe(HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        }

        using (var scope = platform.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.ModuleSessions.SingleAsync(value => value.Id == scenario.SessionId);
            session.Synchronize(
                session.SessionKey,
                session.UserId,
                session.TaskRef,
                session.ReturnUrl,
                DateTimeOffset.UtcNow.AddMinutes(-1),
                session.MaxAttempts);
            await db.SaveChangesAsync();
        }

        using var expired = await SubmitAsync(scenario, scenario.TaskId);
        using var repeated = await SubmitAsync(scenario, scenario.TaskId);
        await ShouldBeProblemAsync(expired, HttpStatusCode.Conflict, "ModuleSession.Expired");
        await ShouldBeProblemAsync(repeated, HttpStatusCode.Conflict, "ModuleSession.Expired");

        using var assertionScope = platform.Services.CreateScope();
        var assertionDb = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await assertionDb.ModuleSessions.AsNoTracking()
            .SingleAsync(value => value.Id == scenario.SessionId);
        stored.Status.ShouldBe(ModuleSessionStatus.Expired);
        stored.FinalizationReason.ShouldBe(Domain.Training.FinalizationReason.Expired);
        stored.FinalScore.ShouldBe(0);
        (await assertionDb.PendingPublishes.CountAsync(value =>
            value.SessionId == scenario.SessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(1);
    }

    [Fact(DisplayName = "Platform scope: первая отправка закрепляет validation version за сессией")]
    public async Task FirstSubmit_PinsValidationVersion()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        ReturnIncorrectResult(scenario);
        using (var first = await SubmitAsync(scenario, scenario.TaskId))
        {
            first.StatusCode.ShouldBe(HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        }

        Guid pinnedVersionId;
        Guid newerVersionId;
        using (var scope = platform.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.ModuleSessions.SingleAsync(value => value.Id == scenario.SessionId);
            pinnedVersionId = session.ValidationVersionId!.Value;
            var pinned = await db.TaskValidationVersions.AsNoTracking()
                .SingleAsync(value => value.Id == pinnedVersionId);
            var task = await db.SqlTasks.SingleAsync(value => value.Id == scenario.TaskId);
            var newer = Domain.Training.TaskValidationVersion.Publish(
                scenario.TaskId,
                pinned.VersionNumber + 1,
                Guid.NewGuid(),
                pinned.PassingScore,
                null,
                pinned.VisibleHintGroupsMask,
                new Domain.Training.TaskValidationVersionSnapshot(
                    pinned.SchemaSnapshotJson,
                    pinned.DatasetSnapshotJson,
                    pinned.ReferenceQuerySnapshotJson,
                    pinned.ExpectedResultSnapshotJson,
                    pinned.ValidationConfigurationSnapshotJson,
                    pinned.AnalyzerVersion),
                Guid.NewGuid(),
                "integration-test",
                DateTimeOffset.UtcNow);
            newerVersionId = newer.Id;
            db.TaskValidationVersions.Add(newer);
            task.ActivateValidationVersion(newer.Id);
            await db.SaveChangesAsync();
        }

        using (var second = await SubmitAsync(scenario, scenario.TaskId))
        {
            second.StatusCode.ShouldBe(HttpStatusCode.Created, await second.Content.ReadAsStringAsync());
        }

        using var assertionScope = platform.Services.CreateScope();
        var assertionDb = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedSession = await assertionDb.ModuleSessions.AsNoTracking()
            .SingleAsync(value => value.Id == scenario.SessionId);
        storedSession.ValidationVersionId.ShouldBe(pinnedVersionId);
        storedSession.ValidationVersionId.ShouldNotBe(newerVersionId);
        var attemptVersions = await assertionDb.Attempts.AsNoTracking()
            .Where(value => value.ModuleSessionId == scenario.SessionId)
            .Select(value => value.ValidationVersionId)
            .ToArrayAsync();
        attemptVersions.ShouldAllBe(value => value == pinnedVersionId);
    }

    [Fact(DisplayName = "Platform scope: токен без session_id в platform-инстансе сохраняет standalone-поведение")]
    public async Task StandaloneToken_KeepsStandaloneBehavior_OnPlatformInstance()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));
        var standaloneUser = Guid.NewGuid();

        using var catalog = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Training.Student.Tasks);
        catalog.Headers.Add("X-Test-UserId", standaloneUser.ToString());
        catalog.Headers.Add("X-Test-Roles", "Student");
        using var catalogResponse = await scenario.Client.SendAsync(catalog);

        using var submit = PlatformProfileAndCatalogTests.CreateSubmitRequest(
            scenario.OtherTaskId, standaloneUser, null);
        using var submitResponse = await scenario.Client.SendAsync(submit);

        catalogResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await submitResponse.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "Platform scope: teacher API закрыт токену с session_id, но открыт без него")]
    public async Task TeacherApi_IsClosedToTokensWithSession()
    {
        using var platform = CreatePlatformApplication();
        var scenario = await SeedAsync(platform, DateTimeOffset.UtcNow.AddMinutes(10));

        using var withSession = await SendAsync(
            scenario, HttpMethod.Get, ApiRoutes.Training.Topics.Collection, role: "Teacher");
        using var withoutSession = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.Training.Topics.Collection);
        withoutSession.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        withoutSession.Headers.Add("X-Test-Roles", "Teacher");
        using var withoutSessionResponse = await scenario.Client.SendAsync(withoutSession);

        withSession.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        withoutSessionResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory(DisplayName = "Platform scope: токен с session_id при выключенной интеграции отклоняется (fail-closed)")]
    [InlineData("tasks")]
    [InlineData("submit")]
    public async Task SessionClaim_WithIntegrationDisabled_IsRejected(string endpoint)
    {
        using var client = app.CreateClient();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var request = endpoint == "tasks"
            ? Request(HttpMethod.Get, ApiRoutes.Training.Student.Tasks, userId, sessionId)
            : PlatformProfileAndCatalogTests.CreateSubmitRequest(Guid.NewGuid(), userId, sessionId);

        using var response = await client.SendAsync(request);

        await ShouldBeProblemAsync(response, HttpStatusCode.Forbidden, "PlatformSession.ScopeRestricted");
    }
}
