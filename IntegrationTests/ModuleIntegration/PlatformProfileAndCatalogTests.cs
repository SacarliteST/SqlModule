using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Host;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.PlatformIntegration.Abstractions;
using SQLModule.PlatformIntegration.Contracts;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Isolated;
using SQLModule.Web.Features.ModuleIntegration;
using Swashbuckle.AspNetCore.Swagger;
using EducationClientOptions = SQLModule.Education.Client.EducationClientOptions;
using EducationCompletionClient = SQLModule.Education.Client.EducationCompletionClient;
using ServiceKeyDelegatingHandler = SQLModule.Education.Client.ServiceKeyDelegatingHandler;

namespace SQLModule.IntegrationTests.ModuleIntegration;

[Collection(IntegrationTestCollection.Name)]
public sealed class PlatformProfileAndCatalogTests(TestApplication app)
{
    private const string ServiceKey = "platform-integration-test-key";

    [Fact(DisplayName = "Standalone-профиль не регистрирует integration endpoints")]
    public async Task StandaloneProfile_DoesNotMapIntegrationEndpoints()
    {
        using var client = app.CreateClient();

        var response = await client.GetAsync(ApiRoutes.ModuleIntegration.TasksCatalog);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        app.Services.GetRequiredService<IOptions<ModuleIntegrationOptions>>()
            .Value.Enabled.ShouldBeFalse();
        app.Services.GetRequiredService<IConfiguration>()["Auth:Audience"]
            .ShouldBe("scoodle-api");
    }

    [Fact(DisplayName = "Platform-профиль включает integration endpoints и отдельный audience")]
    public void PlatformProfile_UsesDedicatedConfiguration()
    {
        using var platform = CreatePlatformApplication();

        platform.Services.GetRequiredService<IOptions<ModuleIntegrationOptions>>()
            .Value.Enabled.ShouldBeTrue();
        platform.Services.GetRequiredService<IConfiguration>()["Auth:Audience"]
            .ShouldBe("sql-module-api");
    }

    [Fact(DisplayName = "Launch-профили закрепляют штатные настройки Platform и Isolated")]
    public void LaunchProfiles_UseExpectedSmokeConfiguration()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        using var launchSettings = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot, "Host", "Properties", "launchSettings.json")));
        using var platformSettings = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot, "Host", "appsettings.Platform.json")));
        using var defaultSettings = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            repositoryRoot, "Host", "appsettings.json")));
        var profiles = launchSettings.RootElement.GetProperty("profiles");
        var platform = profiles.GetProperty("Platform").GetProperty("environmentVariables");
        var isolated = profiles.GetProperty("Isolated").GetProperty("environmentVariables");
        var integration = platformSettings.RootElement.GetProperty("ModuleIntegration");

        platform.GetProperty("ASPNETCORE_ENVIRONMENT").GetString().ShouldBe("Platform");
        platform.GetProperty("UseRobotAuth").GetString().ShouldBe("false");
        platform.GetProperty("UseFakeSandbox").GetString().ShouldBe("false");
        platform.GetProperty("UseAllowAllCors").GetString().ShouldBe("true");
        platform.GetProperty("SeedDemoData").GetString().ShouldBe("false");
        platform.GetProperty("SeedSmokeData").GetString().ShouldBe("true");
        platform.GetProperty("DevTools__Enabled").GetString().ShouldBe("false");
        integration.GetProperty("Enabled").GetBoolean().ShouldBeTrue();
        integration.GetProperty("ServiceKey").GetString()
            .ShouldBe("dev-sql-module-service-key-change-me");
        integration.GetProperty("EducationBaseUrl").GetString().ShouldBe("http://localhost:5135");
        platformSettings.RootElement.GetProperty("Auth").GetProperty("Audience").GetString()
            .ShouldBe("sql-module-api");

        isolated.GetProperty("ASPNETCORE_ENVIRONMENT").GetString().ShouldBe("Development");
        isolated.GetProperty("UseRobotAuth").GetString().ShouldBe("true");
        isolated.GetProperty("UseFakeSandbox").GetString().ShouldBe("true");
        isolated.TryGetProperty("SeedSmokeData", out _).ShouldBeFalse();
        defaultSettings.RootElement.GetProperty("SeedSmokeData").GetBoolean().ShouldBeFalse();
    }

    [Theory(DisplayName = "Platform-профиль отклоняет некорректные настройки доставки при запуске")]
    [InlineData("ModuleIntegration:Kafka:InitialRetryDelaySeconds", "0")]
    [InlineData("ModuleIntegration:Publisher:BatchSize", "0")]
    [InlineData("ModuleIntegration:EducationCompletion:RequestTimeoutSeconds", "0")]
    public void PlatformProfile_RejectsInvalidDeliverySettings(string key, string value)
    {
        using var platform = CreatePlatformApplication(new Dictionary<string, string?>
        {
            [key] = value
        });

        Should.Throw<OptionsValidationException>(() => _ = platform.Services);
    }

    [Fact(DisplayName = "Swagger явно описывает machine-коды интеграционных ошибок")]
    public void Swagger_DescribesIntegrationErrorCodes()
    {
        using var platform = CreatePlatformApplication();
        var swagger = platform.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var submit = swagger.Paths!["/" + ApiRoutes.Training.Attempts.Collection.TrimStart('/')]
            .Operations!.Values.Single(operation => operation.OperationId == "SubmitAttempt");
        var push = swagger.Paths["/" + ApiRoutes.ModuleIntegration.Sessions.TrimStart('/')]
            .Operations!.Values.Single(operation => operation.OperationId == "UpsertModuleSession");

        ((Microsoft.OpenApi.OpenApiResponse)submit.Responses!["403"])
            .Description!.ShouldContain("ModuleSessionForbidden");
        var submitConflict = ((Microsoft.OpenApi.OpenApiResponse)submit.Responses["409"]).Description!;
        submitConflict.ShouldContain("ModuleSessionRequired");
        submitConflict.ShouldContain("SessionTaskMismatch");
        submitConflict.ShouldContain("ModuleSessionClosed");
        submitConflict.ShouldContain("IdempotencyKeyPayloadMismatch");
        submitConflict.ShouldContain("IdempotencyRequestInProgress");
        ((Microsoft.OpenApi.OpenApiResponse)push.Responses!["401"])
            .Description!.ShouldContain("InvalidServiceKey");
        ((Microsoft.OpenApi.OpenApiResponse)push.Responses["409"])
            .Description!.ShouldContain("ModuleSession.Completed");
        ((Microsoft.OpenApi.OpenApiResponse)push.Responses["422"])
            .Description!.ShouldContain("returnUrl");
    }

    [Fact(DisplayName = "Каталог требует service key и возвращает только Published-задания")]
    public async Task TasksCatalog_ReturnsOnlyPublishedTasks()
    {
        var marker = Guid.NewGuid().ToString("N");
        var publishedText = new string('x', 205);
        var taskIds = await SeedTasksAsync(marker, publishedText);
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();

        using var missingKey = await client.GetAsync(ApiRoutes.ModuleIntegration.TasksCatalog);
        missingKey.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.ModuleIntegration.TasksCatalog);
        wrongRequest.Headers.Add("X-Service-Key", "wrong-key");
        using var wrongKey = await client.SendAsync(wrongRequest);
        wrongKey.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.ModuleIntegration.TasksCatalog);
        request.Headers.Add("X-Service-Key", ServiceKey);
        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<List<ModuleTaskCatalogItemResponse>>();
        catalog.ShouldNotBeNull();
        var published = catalog.Single(item => item.Ref == taskIds.Published.ToString());
        published.Name.ShouldBe($"A Published {marker}");
        published.Description.ShouldBe(publishedText[..200]);
        catalog.ShouldNotContain(item => item.Ref == taskIds.Draft.ToString());
        catalog.ShouldNotContain(item => item.Ref == taskIds.Archived.ToString());
    }

    [Fact(DisplayName = "Smoke-seed создаёт стабильное задание, не дублируется и не меняет пользовательские данные")]
    public async Task SmokeSeed_IsIdempotentAndIndependentFromDemoData()
    {
        var userTopicId = Guid.NewGuid();
        var userTopicName = $"User topic {userTopicId:N}";
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Topics.Add(Topic.Create(userTopicName, null, userTopicId));
            await db.SaveChangesAsync();

            await new DemoDataSeeder(db).SeedAsync();
            var seeder = new SmokeDataSeeder(db);
            await seeder.SeedAsync();
            db.ChangeTracker.Clear();

            var countsAfterFirstSeed = await ReadSmokeCountsAsync(db);
            await seeder.SeedAsync();
            db.ChangeTracker.Clear();

            (await ReadSmokeCountsAsync(db)).ShouldBe(countsAfterFirstSeed);
            countsAfterFirstSeed.ShouldAllBe(count => count == 1);
            (await db.Topics.AsNoTracking().SingleAsync(value => value.Id == userTopicId))
                .TopicName.ShouldBe(userTopicName);

            var task = await db.SqlTasks.AsNoTracking()
                .SingleAsync(value => value.Id == SmokeDataSeeder.TaskId);
            task.PublicationStatus.ShouldBe(PublicationStatus.Published);
            task.TaskName.ShouldBe(SmokeDataSeeder.TaskName);
            task.SqlQueryId.ShouldBe(SmokeDataSeeder.QueryId);
            var query = await db.SqlQueries.AsNoTracking()
                .SingleAsync(value => value.Id == SmokeDataSeeder.QueryId);
            query.QueryText.ShouldBe(SmokeDataSeeder.ReferenceSql);
            query.ExpectedResult.ShouldNotBeNull();
            query.ExpectedResult.ShouldContain("\"1\"");
            (await db.CellValues.AsNoTracking().SingleAsync(value => value.Id == SmokeDataSeeder.CellId))
                .TextValue.ShouldBe("1");
        }

        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.ModuleIntegration.TasksCatalog);
        request.Headers.Add("X-Service-Key", ServiceKey);
        using var response = await client.SendAsync(request);
        var catalog = await response.Content.ReadFromJsonAsync<List<ModuleTaskCatalogItemResponse>>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var smokeTask = catalog.ShouldNotBeNull().Single(value => value.Ref == SmokeDataSeeder.TaskId.ToString());
        smokeTask.Name.ShouldBe(SmokeDataSeeder.TaskName);
        smokeTask.Description.ShouldBe("Получите идентификаторы всех пользователей из таблицы users.");
    }

    [Fact(DisplayName = "ModuleSession сохраняет доверенный контекст и lifecycle")]
    public async Task ModuleSession_PersistsTrustedContext()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = ModuleSession.Create(
                sessionId,
                "secret-session-key",
                userId,
                Guid.NewGuid().ToString(),
                "https://platform.example/return",
                expiresAt);
            db.ModuleSessions.Add(session);
            await db.SaveChangesAsync();
            session.MarkCompleted();
            await db.SaveChangesAsync();
        }

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.ModuleSessions.AsNoTracking().SingleAsync(session => session.Id == sessionId);
            stored.UserId.ShouldBe(userId);
            stored.SessionKey.ShouldBe("secret-session-key");
            stored.ReturnUrl.ShouldBe("https://platform.example/return");
            stored.ExpiresAt.ShouldNotBeNull();
            stored.ExpiresAt.Value.ShouldBe(expiresAt, TimeSpan.FromMilliseconds(1));
            stored.Status.ShouldBe(ModuleSessionStatus.Completed);
            stored.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
            stored.UpdatedAt.ShouldBeGreaterThanOrEqualTo(stored.CreatedAt);
        }
    }

    [Fact(DisplayName = "Push платформенной сессии авторизуется, валидируется и выполняет upsert")]
    public async Task ModuleSessionPush_UpsertsActiveSession()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var sessionId = Guid.NewGuid();
        var initial = new UpsertModuleSessionRequest(
            sessionId,
            "initial-session-key",
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "https://platform.example/initial",
            DateTimeOffset.UtcNow.AddMinutes(30));

        using var unauthorized = await client.PostAsJsonAsync(ApiRoutes.ModuleIntegration.Sessions, initial);
        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using (var unauthorizedProblem = JsonDocument.Parse(
                   await unauthorized.Content.ReadAsStringAsync()))
        {
            unauthorizedProblem.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
            unauthorizedProblem.RootElement.GetRawText().ShouldNotContain(initial.SessionKey!);
        }

        using var invalidRequest = CreatePushRequest(new UpsertModuleSessionRequest(
            null, null, null, null, null, null));
        using var invalid = await client.SendAsync(invalidRequest);
        invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        using var createRequest = CreatePushRequest(initial);
        using var created = await client.SendAsync(createRequest);
        created.StatusCode.ShouldBe(HttpStatusCode.OK);

        var changedUserId = Guid.NewGuid();
        var changedExpiry = DateTimeOffset.UtcNow.AddMinutes(45);
        var changed = initial with
        {
            SessionKey = "changed-session-key",
            UserId = changedUserId,
            TaskRef = Guid.NewGuid().ToString(),
            ReturnUrl = "https://platform.example/changed",
            ExpiresAt = changedExpiry
        };
        using var updateRequest = CreatePushRequest(changed);
        using var updated = await client.SendAsync(updateRequest);
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.ModuleSessions.SingleAsync(value => value.Id == sessionId);
        stored.SessionKey.ShouldBe(changed.SessionKey);
        stored.UserId.ShouldBe(changedUserId);
        stored.TaskRef.ShouldBe(changed.TaskRef);
        stored.ReturnUrl.ShouldBe(changed.ReturnUrl);
        stored.ExpiresAt.ShouldNotBeNull();
        stored.ExpiresAt.Value.ShouldBe(changedExpiry, TimeSpan.FromMilliseconds(1));
        stored.Status.ShouldBe(ModuleSessionStatus.Active);
    }

    [Fact(DisplayName = "Повторный push не открывает и не изменяет Completed-сессию")]
    public async Task ModuleSessionPush_RejectsCompletedSession()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var sessionId = Guid.NewGuid();
        var original = new UpsertModuleSessionRequest(
            sessionId,
            "completed-session-key",
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "https://platform.example/completed",
            null);
        using var createRequest = CreatePushRequest(original);
        using var created = await client.SendAsync(createRequest);
        created.StatusCode.ShouldBe(HttpStatusCode.OK);

        using (var scope = platform.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.ModuleSessions.SingleAsync(value => value.Id == sessionId);
            session.MarkCompleted();
            await db.SaveChangesAsync();
        }

        using var replayRequest = CreatePushRequest(original with
        {
            SessionKey = "must-not-be-saved",
            ReturnUrl = "https://attacker.example/redirect"
        });
        using var replay = await client.SendAsync(replayRequest);
        replay.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var verificationScope = platform.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await verificationDb.ModuleSessions.AsNoTracking()
            .SingleAsync(value => value.Id == sessionId);
        stored.SessionKey.ShouldBe(original.SessionKey);
        stored.ReturnUrl.ShouldBe(original.ReturnUrl);
        stored.Status.ShouldBe(ModuleSessionStatus.Completed);
    }

    [Fact(DisplayName = "Параллельные повторы push создают одну платформенную сессию")]
    public async Task ModuleSessionPush_ConcurrentReplayCreatesSingleSession()
    {
        using var platform = CreatePlatformApplication();
        using var firstClient = platform.CreateClient();
        using var secondClient = platform.CreateClient();
        var sessionId = Guid.NewGuid();
        var body = new UpsertModuleSessionRequest(
            sessionId,
            "concurrent-session-key",
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "https://platform.example/concurrent",
            DateTimeOffset.UtcNow.AddMinutes(30));
        using var firstRequest = CreatePushRequest(body);
        using var secondRequest = CreatePushRequest(body);

        var responses = await Task.WhenAll(
            firstClient.SendAsync(firstRequest),
            secondClient.SendAsync(secondRequest));

        try
        {
            responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.ModuleSessions.CountAsync(value => value.Id == sessionId)).ShouldBe(1);
    }

    [Fact(DisplayName = "Student получает канонический контекст сессии из JWT")]
    public async Task CurrentSession_ReturnsTrustedContextForOwner()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(20);
        await SeedModuleSessionAsync(platform, sessionId, userId, taskId, expiresAt);
        using var request = CreateCurrentSessionRequest(sessionId, userId, "Student");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var responseJson = await response.Content.ReadAsStringAsync();
        responseJson.ShouldNotContain("current-session-key");
        responseJson.ShouldNotContain(ServiceKey);
        var current = await response.Content.ReadFromJsonAsync<CurrentModuleSessionResponse>();
        current.ShouldNotBeNull();
        current.TaskId.ShouldBe(taskId);
        current.ReturnUrl.ShouldBe("https://platform.example/return");
        current.ExpiresAt.ShouldNotBeNull();
        current.ExpiresAt.Value.ShouldBe(expiresAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact(DisplayName = "Текущая сессия скрывает отсутствие claim, неизвестную сессию и чужого владельца")]
    public async Task CurrentSession_HidesUnavailableSession()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var sessionId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, sessionId, ownerId, Guid.NewGuid(), null);

        using var missingClaim = CreateCurrentSessionRequest(null, ownerId, "Student");
        using var missingClaimResponse = await client.SendAsync(missingClaim);
        missingClaimResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var unknownSession = CreateCurrentSessionRequest(Guid.NewGuid(), ownerId, "Student");
        using var unknownSessionResponse = await client.SendAsync(unknownSession);
        unknownSessionResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var foreignOwner = CreateCurrentSessionRequest(sessionId, Guid.NewGuid(), "Student");
        using var foreignOwnerResponse = await client.SendAsync(foreignOwner);
        foreignOwnerResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory(DisplayName = "Текущая сессия доступна только Student")]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Teacher", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.Forbidden)]
    public async Task CurrentSession_EnforcesStudentPolicy(string? role, HttpStatusCode expectedStatus)
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        using var request = CreateCurrentSessionRequest(Guid.NewGuid(), Guid.NewGuid(), role);

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(expectedStatus);
    }

    [Fact(DisplayName = "Первая правильная platform-попытка ставит оценку в очередь и ожидает подтверждения")]
    public async Task PlatformSubmit_FirstCorrectAttempt_QueuesGradeAndWaitsForCompletion()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var userId = Guid.NewGuid();
        var taskId = await SeedRunnableTaskAsync(platform);
        var sessionId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, sessionId, userId, taskId, DateTimeOffset.UtcNow.AddMinutes(10));
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();
        using var request = CreateSubmitRequest(taskId, userId, sessionId);

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        executor.RunCallCount.ShouldBe(1);
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = await db.Attempts.AsNoTracking().SingleAsync(value => value.TaskId == taskId);
        attempt.UserId.ShouldBe(userId);
        attempt.ModuleSessionId.ShouldBe(sessionId);
        var session = await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId);
        session.Status.ShouldBe(ModuleSessionStatus.CompletionPending);
        var pending = await db.PendingPublishes.AsNoTracking()
            .Where(value => value.SessionId == sessionId)
            .OrderBy(value => value.Kind)
            .ToListAsync();
        pending.Count.ShouldBe(2);
        var eventMessage = pending.Single(value => value.Kind == PendingPublishKind.Event);
        eventMessage.DeduplicationKey.ShouldBe($"event:{attempt.Id:D}");
        eventMessage.Attempts.ShouldBe(0);
        eventMessage.SentAt.ShouldBeNull();
        eventMessage.DeadLetterAt.ShouldBeNull();
        using var message = JsonDocument.Parse(eventMessage.MessageJson);
        var root = message.RootElement;
        root.GetProperty("sessionId").GetGuid().ShouldBe(sessionId);
        root.GetProperty("sessionKey").GetString().ShouldBe("current-session-key");
        root.GetProperty("eventId").GetGuid().ShouldBe(eventMessage.Id);
        root.GetProperty("kind").GetString().ShouldBe("sql_submit");
        root.GetProperty("payload").GetProperty("status").GetString().ShouldBe("SUCCESS");
        root.GetProperty("payload").GetProperty("isCorrect").GetBoolean().ShouldBeTrue();

        var gradeMessage = pending.Single(value => value.Kind == PendingPublishKind.Grade);
        gradeMessage.DeduplicationKey.ShouldBe($"grade:{sessionId:D}");
        using var grade = JsonDocument.Parse(gradeMessage.MessageJson);
        grade.RootElement.GetProperty("sessionKey").GetString().ShouldBe("current-session-key");
        grade.RootElement.GetProperty("grade").GetInt32().ShouldBe(100);
        grade.RootElement.GetProperty("completionData").GetProperty("totalAttempts").GetInt32().ShouldBe(1);
        grade.RootElement.GetProperty("completionData").GetProperty("correctAttemptId").GetGuid()
            .ShouldBe(attempt.Id);
    }

    [Fact(DisplayName = "Неправильная попытка оставляет сессию активной, а правильная учитывает общее число попыток")]
    public async Task PlatformSubmit_IncorrectThenCorrect_QueuesSingleGradeWithAttemptCount()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var userId = Guid.NewGuid();
        var taskId = await SeedRunnableTaskAsync(platform);
        var sessionId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, sessionId, userId, taskId, DateTimeOffset.UtcNow.AddMinutes(10));
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.OverrideRun = global::SQLModule.Common.Results.Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, ["unexpected"], [], 0, 1));

        try
        {
            using var incorrectRequest = CreateSubmitRequest(taskId, userId, sessionId);
            using var incorrectResponse = await client.SendAsync(incorrectRequest);
            incorrectResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

            using (var scope = platform.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId))
                    .Status.ShouldBe(ModuleSessionStatus.Active);
                (await db.PendingPublishes.CountAsync(value =>
                    value.SessionId == sessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(0);
            }

            executor.OverrideRun = null;
            using var correctRequest = CreateSubmitRequest(taskId, userId, sessionId);
            using var correctResponse = await client.SendAsync(correctRequest);
            correctResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

            using var verificationScope = platform.Services.CreateScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gradeMessage = await verificationDb.PendingPublishes.AsNoTracking().SingleAsync(value =>
                value.SessionId == sessionId && value.Kind == PendingPublishKind.Grade);
            var eventIds = await verificationDb.PendingPublishes.AsNoTracking()
                .Where(value => value.SessionId == sessionId && value.Kind == PendingPublishKind.Event)
                .Select(value => value.Id)
                .ToListAsync();
            eventIds.Count.ShouldBe(2);
            eventIds.Distinct().Count().ShouldBe(2);
            using var grade = JsonDocument.Parse(gradeMessage.MessageJson);
            grade.RootElement.GetProperty("completionData").GetProperty("totalAttempts").GetInt32().ShouldBe(2);
        }
        finally
        {
            executor.OverrideRun = null;
        }
    }

    [Fact(DisplayName = "Повтор правильной попытки с тем же ключом идемпотентности возвращает исходный результат")]
    public async Task PlatformSubmit_RepeatedCorrectRequest_DoesNotDuplicateAttemptOrGrade()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var userId = Guid.NewGuid();
        var taskId = await SeedRunnableTaskAsync(platform);
        var sessionId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        await SeedModuleSessionAsync(platform, sessionId, userId, taskId, DateTimeOffset.UtcNow.AddMinutes(10));
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();

        using var firstRequest = CreateSubmitRequest(taskId, userId, sessionId, idempotencyKey);
        using var firstResponse = await client.SendAsync(firstRequest);
        using var repeatedRequest = CreateSubmitRequest(taskId, userId, sessionId, idempotencyKey);
        using var repeatedResponse = await client.SendAsync(repeatedRequest);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        repeatedResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        executor.RunCallCount.ShouldBe(1);
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == sessionId)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == sessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(1);
    }

    [Fact(DisplayName = "Новый submit после завершения сессии отклоняется без запуска sandbox")]
    public async Task PlatformSubmit_AfterCorrectAttempt_RejectsNewRequest()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var userId = Guid.NewGuid();
        var taskId = await SeedRunnableTaskAsync(platform);
        var sessionId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, sessionId, userId, taskId, DateTimeOffset.UtcNow.AddMinutes(10));
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();

        using var firstRequest = CreateSubmitRequest(taskId, userId, sessionId);
        using var firstResponse = await client.SendAsync(firstRequest);
        using var secondRequest = CreateSubmitRequest(taskId, userId, sessionId);
        using var secondResponse = await client.SendAsync(secondRequest);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        executor.RunCallCount.ShouldBe(1);
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == sessionId)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == sessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(1);
    }

    [Fact(DisplayName = "Параллельные правильные submit создают только одну попытку и одну оценку")]
    public async Task PlatformSubmit_ConcurrentCorrectRequests_CompleteSessionOnce()
    {
        using var platform = CreatePlatformApplication();
        using var firstClient = platform.CreateClient();
        using var secondClient = platform.CreateClient();
        var userId = Guid.NewGuid();
        var taskId = await SeedRunnableTaskAsync(platform);
        var sessionId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, sessionId, userId, taskId, DateTimeOffset.UtcNow.AddMinutes(10));
        using var firstRequest = CreateSubmitRequest(taskId, userId, sessionId);
        using var secondRequest = CreateSubmitRequest(taskId, userId, sessionId);

        var responses = await Task.WhenAll(
            firstClient.SendAsync(firstRequest),
            secondClient.SendAsync(secondRequest));

        try
        {
            responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);
            responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.ModuleSessionId == sessionId)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == sessionId && value.Kind == PendingPublishKind.Event)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.SessionId == sessionId && value.Kind == PendingPublishKind.Grade)).ShouldBe(1);
        (await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId))
            .Status.ShouldBe(ModuleSessionStatus.CompletionPending);
    }

    [Fact(DisplayName = "Standalone submit не создаёт integration outbox")]
    public async Task StandaloneSubmit_DoesNotCreatePendingPublish()
    {
        using var client = app.CreateClient();
        var userId = Guid.NewGuid();
        var taskId = await SeedRunnableTaskAsync(app);
        var executor = (FakeSandboxExecutor)app.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();
        int before;
        using (var scope = app.Services.CreateScope())
        {
            before = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .PendingPublishes.CountAsync();
        }

        using var request = CreateSubmitRequest(taskId, userId, null);
        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var verificationScope = app.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.PendingPublishes.CountAsync()).ShouldBe(before);
    }

    [Fact(DisplayName = "Platform submit без известной сессии отклоняется до sandbox")]
    public async Task PlatformSubmit_WithoutSession_RejectsBeforeSandbox()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();
        var userId = Guid.NewGuid();

        using var missingClaimRequest = CreateSubmitRequest(Guid.NewGuid(), userId, null);
        using var missingClaim = await client.SendAsync(missingClaimRequest);
        missingClaim.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var unknownSessionRequest = CreateSubmitRequest(Guid.NewGuid(), userId, Guid.NewGuid());
        using var unknownSession = await client.SendAsync(unknownSessionRequest);
        unknownSession.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        executor.RunCallCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Platform submit проверяет владельца и задание сессии до sandbox")]
    public async Task PlatformSubmit_ValidatesOwnerAndTaskBeforeSandbox()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();
        var ownerId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, sessionId, ownerId, taskId, null);

        using var foreignOwnerRequest = CreateSubmitRequest(taskId, Guid.NewGuid(), sessionId);
        using var foreignOwner = await client.SendAsync(foreignOwnerRequest);
        foreignOwner.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var wrongTaskRequest = CreateSubmitRequest(Guid.NewGuid(), ownerId, sessionId);
        using var wrongTask = await client.SendAsync(wrongTaskRequest);
        wrongTask.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        executor.RunCallCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Platform submit запрещён для всех неактивных и просроченных сессий")]
    public async Task PlatformSubmit_RejectsClosedSessionBeforeSandbox()
    {
        using var platform = CreatePlatformApplication();
        using var client = platform.CreateClient();
        var executor = (FakeSandboxExecutor)platform.Services.GetRequiredService<ISandboxExecutor>();
        executor.ResetRunCallCount();
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var completedId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();
        var failedId = Guid.NewGuid();
        var expiredId = Guid.NewGuid();
        await SeedModuleSessionAsync(platform, completedId, userId, taskId, null);
        await SeedModuleSessionAsync(platform, pendingId, userId, taskId, null);
        await SeedModuleSessionAsync(platform, failedId, userId, taskId, null);
        await SeedModuleSessionAsync(platform, expiredId, userId, taskId, DateTimeOffset.UtcNow.AddMinutes(-1));
        using (var scope = platform.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var completed = await db.ModuleSessions.SingleAsync(value => value.Id == completedId);
            completed.MarkCompleted();
            var pending = await db.ModuleSessions.SingleAsync(value => value.Id == pendingId);
            pending.MarkCompletionPending();
            var failed = await db.ModuleSessions.SingleAsync(value => value.Id == failedId);
            failed.MarkCompletionPending();
            failed.MarkCompletionFailed();
            await db.SaveChangesAsync();
        }

        using var completedRequest = CreateSubmitRequest(taskId, userId, completedId);
        using var completedResponse = await client.SendAsync(completedRequest);
        completedResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var pendingRequest = CreateSubmitRequest(taskId, userId, pendingId);
        using var pendingResponse = await client.SendAsync(pendingRequest);
        pendingResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var failedRequest = CreateSubmitRequest(taskId, userId, failedId);
        using var failedResponse = await client.SendAsync(failedRequest);
        failedResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var expiredRequest = CreateSubmitRequest(taskId, userId, expiredId);
        using var expiredResponse = await client.SendAsync(expiredRequest);
        expiredResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        executor.RunCallCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Lifecycle удаляет только устаревшие сессии и сохраняет историю попыток")]
    public async Task ModuleSessionCleanup_RemovesStaleSessionsAndDetachesAttempts()
    {
        using var platform = CreatePlatformApplication();
        var taskId = await SeedRunnableTaskAsync(platform);
        var now = DateTimeOffset.UtcNow;
        var oldExpiredId = Guid.NewGuid();
        var recentExpiredId = Guid.NewGuid();
        var activeId = Guid.NewGuid();
        var completedId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var oldExpired = ModuleSession.Create(
            oldExpiredId, "old-secret", Guid.NewGuid(), taskId.ToString(),
            "https://platform.example/old", now.AddDays(-8));
        var recentExpired = ModuleSession.Create(
            recentExpiredId, "recent-secret", Guid.NewGuid(), taskId.ToString(),
            "https://platform.example/recent", now.AddDays(-1));
        var active = ModuleSession.Create(
            activeId, "active-secret", Guid.NewGuid(), taskId.ToString(),
            "https://platform.example/active", null);
        var completed = ModuleSession.Create(
            completedId, "completed-secret", Guid.NewGuid(), taskId.ToString(),
            "https://platform.example/completed", null);
        completed.MarkCompleted();
        db.ModuleSessions.AddRange(oldExpired, recentExpired, active, completed);
        db.Attempts.Add(Attempt.Record(
            oldExpired.UserId,
            taskId,
            "SELECT 0",
            ExecutionStatus.Succeeded,
            false,
            CheckReason.ValueMismatch,
            0,
            1,
            null,
            now.AddMinutes(-1),
            now,
            attemptId,
            moduleSessionId: oldExpiredId));
        await db.SaveChangesAsync();
        await db.ModuleSessions
            .Where(session => session.Id == completedId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                session => session.UpdatedAt,
                now.AddDays(-8)));
        db.ChangeTracker.Clear();
        var processor = new ModuleSessionCleanupProcessor(
            db,
            Options.Create(new ModuleIntegrationOptions
            {
                Enabled = true,
                Lifecycle = new ModuleSessionLifecycleOptions
                {
                    CleanupIntervalMinutes = 60,
                    RetentionDays = 7,
                    BatchSize = 100
                }
            }),
            TimeProvider.System,
            NullLogger<ModuleSessionCleanupProcessor>.Instance);

        var removed = await processor.CleanupAsync(CancellationToken.None);

        removed.ShouldBe(1);
        (await db.ModuleSessions.AsNoTracking().SingleAsync(session => session.Id == oldExpiredId))
            .Status.ShouldBe(ModuleSessionStatus.Expired);
        (await db.ModuleSessions.AnyAsync(session => session.Id == completedId)).ShouldBeFalse();
        (await db.ModuleSessions.AsNoTracking().SingleAsync(session => session.Id == recentExpiredId))
            .Status.ShouldBe(ModuleSessionStatus.Expired);
        (await db.ModuleSessions.AsNoTracking().SingleAsync(session => session.Id == activeId))
            .Status.ShouldBe(ModuleSessionStatus.Active);
        (await db.PendingPublishes.CountAsync(message =>
            message.SessionId == oldExpiredId || message.SessionId == recentExpiredId)).ShouldBe(0);
        (await db.Attempts.AsNoTracking().SingleAsync(value => value.Id == attemptId))
            .ModuleSessionId.ShouldBe(oldExpiredId);

        await db.ModuleSessions
            .Where(session => session.Id == oldExpiredId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                session => session.UpdatedAt,
                now.AddDays(-8)));
        db.ChangeTracker.Clear();

        (await processor.CleanupAsync(CancellationToken.None)).ShouldBe(1);
        (await db.ModuleSessions.AnyAsync(session => session.Id == oldExpiredId)).ShouldBeFalse();
        (await db.Attempts.AsNoTracking().SingleAsync(value => value.Id == attemptId))
            .ModuleSessionId.ShouldBeNull();
    }

    [Fact(DisplayName = "Publisher подтверждает успешное событие и не блокируется ошибочным")]
    public async Task PendingPublisher_ProcessesMessagesIndependently()
    {
        var now = DateTimeOffset.UtcNow.AddSeconds(-1);
        var failedId = Guid.NewGuid();
        var successfulId = Guid.NewGuid();
        var failedSessionId = Guid.NewGuid();
        var successfulSessionId = Guid.NewGuid();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.PendingPublishes.AddRange(
            PendingPublish.Create(failedId, PendingPublishKind.Event, failedSessionId,
                $"event:{failedId}", CreateEventMessageJson(failedSessionId, "fail"), now),
            PendingPublish.Create(successfulId, PendingPublishKind.Event, successfulSessionId,
                $"event:{successfulId}", CreateEventMessageJson(successfulSessionId, "ok"), now));
        await db.SaveChangesAsync();
        var publisher = new RecordingEventPublisher("fail");
        var processor = CreatePendingPublishProcessor(db, publisher);

        (await processor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(2);

        var failed = await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == failedId);
        failed.Attempts.ShouldBe(1);
        failed.SentAt.ShouldBeNull();
        failed.DeadLetterAt.ShouldBeNull();
        failed.NextAttemptAt.ShouldBeGreaterThan(now);
        var successful = await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == successfulId);
        successful.SentAt.ShouldNotBeNull();
        publisher.PublishedIds.ShouldContain(successful.SessionId);
    }

    [Fact(DisplayName = "Publisher после настроенного числа ошибок переводит событие в dead-letter")]
    public async Task PendingPublisher_MovesPoisonMessageToDeadLetter()
    {
        var now = DateTimeOffset.UtcNow.AddSeconds(-1);
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = PendingPublish.Create(
            messageId, PendingPublishKind.Event, sessionId,
            $"event:{messageId}", CreateEventMessageJson(sessionId, "fail"), now);
        for (var attempt = 0; attempt < 9; attempt++)
        {
            message.RegisterFailure(now, now, false);
        }

        db.PendingPublishes.Add(message);
        await db.SaveChangesAsync();
        var processor = CreatePendingPublishProcessor(db, new RecordingEventPublisher("fail"));

        await processor.ProcessBatchAsync(CancellationToken.None);

        var stored = await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == messageId);
        stored.Attempts.ShouldBe(10);
        stored.DeadLetterAt.ShouldNotBeNull();
        stored.SentAt.ShouldBeNull();
    }

    [Fact(DisplayName = "Исключение доставки не переносит секрет из сообщения в application log")]
    public async Task PendingPublisher_DoesNotLogSecretFromDeliveryException()
    {
        const string secret = "log-forbidden-session-key";
        var now = DateTimeOffset.UtcNow.AddSeconds(-1);
        var messageId = Guid.NewGuid();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.PendingPublishes.Add(PendingPublish.Create(
            messageId,
            PendingPublishKind.Event,
            Guid.NewGuid(),
            $"event:{messageId}",
            CreateEventMessageJson(Guid.NewGuid(), secret, secret),
            now));
        await db.SaveChangesAsync();
        var logger = new RecordingLogger<PendingPublishProcessor>();
        var processor = CreatePendingPublishProcessor(
            db,
            new RecordingEventPublisher(secret, secret),
            logger: logger);

        await processor.ProcessBatchAsync(CancellationToken.None);

        logger.Entries.ShouldNotBeEmpty();
        logger.Entries.ShouldAllBe(entry => !entry.Contains(secret, StringComparison.Ordinal));
    }

    [Theory(DisplayName = "Доставка оценки переводит ожидающую сессию в терминальное состояние")]
    [InlineData(
        (int)EducationCompletionDeliveryResult.Accepted,
        (int)ModuleSessionStatus.Completed,
        true)]
    [InlineData(
        (int)EducationCompletionDeliveryResult.TerminalConflict,
        (int)ModuleSessionStatus.Completed,
        true)]
    [InlineData(
        (int)EducationCompletionDeliveryResult.AuthenticationRejected,
        (int)ModuleSessionStatus.CompletionFailed,
        false)]
    [InlineData(
        (int)EducationCompletionDeliveryResult.NonRetryableRejection,
        (int)ModuleSessionStatus.CompletionFailed,
        false)]
    public async Task PendingPublisher_GradeResult_TransitionsSession(
        int deliveryResult,
        int expectedStatus,
        bool expectedSent)
    {
        var now = DateTimeOffset.UnixEpoch.AddDays(2);
        var sessionId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = ModuleSession.Create(
            sessionId,
            "transition-session-key",
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "https://platform.example/transition",
            null);
        session.MarkCompletionPending();
        db.ModuleSessions.Add(session);
        db.PendingPublishes.Add(PendingPublish.Create(
            messageId,
            PendingPublishKind.Grade,
            sessionId,
            $"grade:{sessionId}",
            "{\"grade\":100}",
            now));
        await db.SaveChangesAsync();
        var processor = CreatePendingPublishProcessor(
            db,
            new RecordingEventPublisher("never-fail"),
            new StubCompletionClient((EducationCompletionDeliveryResult)deliveryResult),
            new MutableTimeProvider(now));

        await processor.ProcessBatchAsync(CancellationToken.None);

        db.ChangeTracker.Clear();
        var storedSession = await db.ModuleSessions.AsNoTracking()
            .SingleAsync(value => value.Id == sessionId);
        storedSession.Status.ShouldBe((ModuleSessionStatus)expectedStatus);
        var storedMessage = await db.PendingPublishes.AsNoTracking()
            .SingleAsync(value => value.Id == messageId);
        (storedMessage.SentAt is not null).ShouldBe(expectedSent);
        (storedMessage.DeadLetterAt is not null).ShouldBe(!expectedSent);
    }

    [Fact(DisplayName = "Kafka и Education используют независимые retry-настройки")]
    public async Task PendingPublisher_UsesSeparateRetryPoliciesByMessageKind()
    {
        var now = DateTimeOffset.UnixEpoch.AddDays(3);
        var timeProvider = new MutableTimeProvider(now);
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = ModuleSession.Create(
            sessionId,
            "separate-retry-session-key",
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "https://platform.example/retry",
            null);
        session.MarkCompletionPending();
        db.ModuleSessions.Add(session);
        db.PendingPublishes.AddRange(
            PendingPublish.Create(
                eventId, PendingPublishKind.Event, sessionId, $"event:{eventId}",
                CreateEventMessageJson(sessionId, "fail"), now),
            PendingPublish.Create(
                gradeId, PendingPublishKind.Grade, sessionId, $"grade:{sessionId}",
                "{\"grade\":100}", now));
        await db.SaveChangesAsync();
        var integrationOptions = Options.Create(new ModuleIntegrationOptions
        {
            Enabled = true,
            ServiceKey = ServiceKey,
            EducationBaseUrl = "http://education.test",
            Kafka = new ModuleIntegrationKafkaOptions
            {
                BootstrapServers = "kafka.test:9092",
                EventsTopic = "scoodle.practice.events",
                PublisherEnabled = true,
                MaxAttempts = 2,
                InitialRetryDelaySeconds = 3,
                MaxRetryDelaySeconds = 12
            },
            Publisher = new ModuleIntegrationPublisherOptions
            {
                PollIntervalSeconds = 1,
                BatchSize = 20,
                SentRetentionDays = 14
            },
            EducationCompletion = new ModuleIntegrationEducationCompletionOptions
            {
                MaxAttempts = 3,
                InitialRetryDelaySeconds = 7,
                MaxRetryDelaySeconds = 20,
                RequestTimeoutSeconds = 10
            }
        });
        var processor = CreatePendingPublishProcessor(
            db,
            new RecordingEventPublisher("fail"),
            new StubCompletionClient(shouldThrow: true),
            timeProvider,
            integrationOptions);

        (await processor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(2);
        db.ChangeTracker.Clear();
        var firstEvent = await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == eventId);
        var firstGrade = await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == gradeId);
        firstEvent.NextAttemptAt.ShouldBe(now.AddSeconds(3));
        firstGrade.NextAttemptAt.ShouldBe(now.AddSeconds(7));

        timeProvider.Advance(TimeSpan.FromSeconds(3));
        (await processor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(1);
        db.ChangeTracker.Clear();
        (await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == eventId))
            .DeadLetterAt.ShouldNotBeNull();
        (await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId))
            .Status.ShouldBe(ModuleSessionStatus.CompletionPending);

        timeProvider.Advance(TimeSpan.FromSeconds(4));
        (await processor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(1);
        db.ChangeTracker.Clear();
        var secondGrade = await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == gradeId);
        secondGrade.Attempts.ShouldBe(2);
        secondGrade.NextAttemptAt.ShouldBe(now.AddSeconds(21));

        timeProvider.Advance(TimeSpan.FromSeconds(14));
        (await processor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(1);
        db.ChangeTracker.Clear();
        (await db.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == gradeId))
            .DeadLetterAt.ShouldNotBeNull();
        (await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId))
            .Status.ShouldBe(ModuleSessionStatus.CompletionFailed);
    }

    [Fact(DisplayName = "Outbox после рестарта повторяет Kafka-событие и оценку Education")]
    public async Task PendingPublisher_AfterRestart_RetriesPersistedEventAndGrade()
    {
        var now = DateTimeOffset.UnixEpoch.AddDays(1);
        var timeProvider = new MutableTimeProvider(now);
        var sessionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        using (var firstScope = app.Services.CreateScope())
        {
            var db = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = ModuleSession.Create(
                sessionId,
                "restart-session-key",
                Guid.NewGuid(),
                Guid.NewGuid().ToString(),
                "https://platform.example/restart",
                now.AddHours(1));
            session.MarkCompletionPending();
            db.ModuleSessions.Add(session);
            db.PendingPublishes.AddRange(
                PendingPublish.Create(
                    eventId,
                    PendingPublishKind.Event,
                    sessionId,
                    $"event:{eventId}",
                    CreateEventMessageJson(sessionId, "fail"),
                    now),
                PendingPublish.Create(
                    gradeId,
                    PendingPublishKind.Grade,
                    sessionId,
                    $"grade:{sessionId}",
                    "{\"grade\":100}",
                    now));
            await db.SaveChangesAsync();
            var failingProcessor = CreatePendingPublishProcessor(
                db,
                new RecordingEventPublisher("fail"),
                new StubCompletionClient(shouldThrow: true),
                timeProvider);

            (await failingProcessor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(2);

            var failed = await db.PendingPublishes.AsNoTracking()
                .Where(message => message.SessionId == sessionId)
                .ToListAsync();
            failed.ShouldAllBe(message => message.Attempts == 1);
            failed.ShouldAllBe(message => message.SentAt == null);
            failed.ShouldAllBe(message => message.DeadLetterAt == null);
            (await db.ModuleSessions.AsNoTracking().SingleAsync(value => value.Id == sessionId))
                .Status.ShouldBe(ModuleSessionStatus.CompletionPending);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(3));
        var successfulPublisher = new RecordingEventPublisher("never-fail");
        var successfulCompletion = new StubCompletionClient();
        using (var restartedScope = app.Services.CreateScope())
        {
            var restartedDb = restartedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var restartedProcessor = CreatePendingPublishProcessor(
                restartedDb,
                successfulPublisher,
                successfulCompletion,
                timeProvider);

            (await restartedProcessor.ProcessBatchAsync(CancellationToken.None)).ShouldBe(2);

            var delivered = await restartedDb.PendingPublishes.AsNoTracking()
                .Where(message => message.SessionId == sessionId)
                .ToListAsync();
            delivered.ShouldAllBe(message => message.Attempts == 1);
            delivered.ShouldAllBe(message => message.SentAt != null);
            delivered.ShouldAllBe(message => message.DeadLetterAt == null);
            (await restartedDb.ModuleSessions.AsNoTracking().SingleAsync(session => session.Id == sessionId))
                .Status.ShouldBe(ModuleSessionStatus.Completed);
        }

        successfulPublisher.PublishedIds.ShouldContain(sessionId);
        successfulCompletion.Calls.ShouldBe(1);
    }

    [Theory(DisplayName = "Education completion client классифицирует terminal HTTP-ответы")]
    [InlineData(HttpStatusCode.OK, (int)EducationCompletionDeliveryResult.Accepted)]
    [InlineData(HttpStatusCode.Conflict, (int)EducationCompletionDeliveryResult.TerminalConflict)]
    [InlineData(HttpStatusCode.Unauthorized, (int)EducationCompletionDeliveryResult.AuthenticationRejected)]
    [InlineData(HttpStatusCode.BadRequest, (int)EducationCompletionDeliveryResult.NonRetryableRejection)]
    public async Task CompletionClient_ClassifiesTerminalResponses(
        HttpStatusCode status,
        int expected)
    {
        var handler = new StaticResponseHandler(status);
        using var httpClient = new HttpClient(new ServiceKeyDelegatingHandler(
            CreateEducationClientOptions())
        {
            InnerHandler = handler,
        })
        { BaseAddress = new Uri("http://education.test/") };
        var client = new EducationCompletionClient(
            httpClient,
            NullLogger<EducationCompletionClient>.Instance);
        var sessionId = Guid.NewGuid();
        var request = CreateCompletionRequest();

        var result = await client.CompleteAsync(sessionId, request, CancellationToken.None);

        result.ShouldBe((EducationCompletionDeliveryResult)expected);
        handler.RequestPath.ShouldBe($"/api/v1/module-sessions/{sessionId:D}/complete");
        handler.ServiceKey.ShouldBe(ServiceKey);
        handler.Body.ShouldBe(JsonSerializer.Serialize(request, PlatformIntegrationJson.Default));
    }

    [Fact(DisplayName = "Education completion client считает 5xx временной ошибкой")]
    public async Task CompletionClient_ThrowsForServerFailure()
    {
        using var httpClient = new HttpClient(new ServiceKeyDelegatingHandler(
            CreateEducationClientOptions())
        {
            InnerHandler = new StaticResponseHandler(HttpStatusCode.ServiceUnavailable),
        })
        { BaseAddress = new Uri("http://education.test/") };
        var client = new EducationCompletionClient(
            httpClient,
            NullLogger<EducationCompletionClient>.Instance);

        await Should.ThrowAsync<HttpRequestException>(() => client.CompleteAsync(
            Guid.NewGuid(), CreateCompletionRequest(), CancellationToken.None));
    }

    private WebApplicationFactory<IHostMarker> CreatePlatformApplication(
        IReadOnlyDictionary<string, string?>? overrides = null) =>
        app.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Platform");
            builder.ConfigureAppConfiguration(configuration =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ModuleIntegration:Enabled"] = "true",
                    ["ModuleIntegration:ServiceKey"] = ServiceKey,
                    ["ModuleIntegration:EducationBaseUrl"] = "http://education.test",
                    ["ModuleIntegration:Kafka:BootstrapServers"] = "kafka.test:9092",
                    ["ModuleIntegration:Kafka:EventsTopic"] = "scoodle.practice.events",
                    ["ModuleIntegration:Kafka:PublisherEnabled"] = "false"
                };
                if (overrides is not null)
                {
                    foreach (var pair in overrides)
                    {
                        values[pair.Key] = pair.Value;
                    }
                }

                configuration.AddInMemoryCollection(values);
            });
        });

    private static HttpRequestMessage CreatePushRequest(UpsertModuleSessionRequest request)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.ModuleIntegration.Sessions)
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-Service-Key", ServiceKey);
        return message;
    }

    private static HttpRequestMessage CreateCurrentSessionRequest(
        Guid? sessionId,
        Guid userId,
        string? role)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, ApiRoutes.ModuleIntegration.CurrentSession);
        message.Headers.Add("X-Test-UserId", userId.ToString());
        if (role is null)
        {
            message.Headers.Add("X-Test-Anonymous", "true");
        }
        else
        {
            message.Headers.Add("X-Test-Roles", role);
        }

        if (sessionId.HasValue)
        {
            message.Headers.Add("X-Test-SessionId", sessionId.Value.ToString());
        }

        return message;
    }

    private static HttpRequestMessage CreateSubmitRequest(
        Guid taskId,
        Guid userId,
        Guid? sessionId,
        string? idempotencyKey = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(new Contracts.Training.Attempt.SubmitAttemptRequest(
                taskId, "SELECT 1"))
        };
        message.Headers.Add("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        message.Headers.Add("X-Test-UserId", userId.ToString());
        message.Headers.Add("X-Test-Roles", "Student");
        if (sessionId.HasValue)
        {
            message.Headers.Add("X-Test-SessionId", sessionId.Value.ToString());
        }

        return message;
    }

    private static async Task SeedModuleSessionAsync(
        WebApplicationFactory<IHostMarker> platform,
        Guid sessionId,
        Guid userId,
        Guid taskId,
        DateTimeOffset? expiresAt)
    {
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ModuleSessions.Add(ModuleSession.Create(
            sessionId,
            "current-session-key",
            userId,
            taskId.ToString(),
            "https://platform.example/return",
            expiresAt));
        await db.SaveChangesAsync();
    }

    private static async Task<int[]> ReadSmokeCountsAsync(AppDbContext db) =>
    [
        await db.DbmsDictionaries.CountAsync(value => value.Id == SmokeDataSeeder.DbmsId),
        await db.PhysicalTypes.CountAsync(value => value.Id == SmokeDataSeeder.IntegerTypeId),
        await db.TargetDbs.CountAsync(value => value.Id == SmokeDataSeeder.TargetDbId),
        await db.MetaTables.CountAsync(value => value.Id == SmokeDataSeeder.TableId),
        await db.MetaAttributes.CountAsync(value => value.Id == SmokeDataSeeder.AttributeId),
        await db.DataRecords.CountAsync(value => value.Id == SmokeDataSeeder.RecordId),
        await db.CellValues.CountAsync(value => value.Id == SmokeDataSeeder.CellId),
        await db.Topics.CountAsync(value => value.Id == SmokeDataSeeder.TopicId),
        await db.SqlQueries.CountAsync(value => value.Id == SmokeDataSeeder.QueryId),
        await db.SqlTasks.CountAsync(value => value.Id == SmokeDataSeeder.TaskId)
    ];

    private static async Task<Guid> SeedRunnableTaskAsync(
        WebApplicationFactory<IHostMarker> platform)
    {
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var dbms = DbmsDictionary.Create(
            $"Submit {marker}", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var targetDb = TargetDb.Create(dbms.Id, $"submit_{marker}", null, false);
        var topic = Topic.Create($"Submit {marker}");
        var query = SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        query.SetExpectedResult("{\"columns\":[],\"rows\":[]}");
        var task = SqlTask.Create(
            topic.Id, query.Id, $"Submit {marker}", "Runnable platform task", 1,
            publicationStatus: PublicationStatus.Published);
        db.AddRange(dbms, targetDb, topic, query, task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    private static PendingPublishProcessor CreatePendingPublishProcessor(
        AppDbContext db,
        IPracticeEventPublisher publisher,
        IEducationCompletionClient? completionClient = null,
        TimeProvider? timeProvider = null,
        IOptions<ModuleIntegrationOptions>? integrationOptions = null,
        ILogger<PendingPublishProcessor>? logger = null) =>
        new(
            db,
            publisher,
            completionClient ?? new StubCompletionClient(),
            integrationOptions ?? CreateModuleIntegrationOptions(),
            timeProvider ?? TimeProvider.System,
            logger ?? NullLogger<PendingPublishProcessor>.Instance);

    private static IOptions<ModuleIntegrationOptions> CreateModuleIntegrationOptions() =>
        Options.Create(new ModuleIntegrationOptions
        {
            Enabled = true,
            ServiceKey = ServiceKey,
            EducationBaseUrl = "http://education.test",
            Kafka = new ModuleIntegrationKafkaOptions
            {
                BootstrapServers = "kafka.test:9092",
                EventsTopic = "scoodle.practice.events",
                PublisherEnabled = true,
                MaxAttempts = 10,
                InitialRetryDelaySeconds = 2,
                MaxRetryDelaySeconds = 60
            },
            Publisher = new ModuleIntegrationPublisherOptions
            {
                PollIntervalSeconds = 1,
                BatchSize = 20,
                SentRetentionDays = 7
            },
            EducationCompletion = new ModuleIntegrationEducationCompletionOptions
            {
                MaxAttempts = 10,
                InitialRetryDelaySeconds = 2,
                MaxRetryDelaySeconds = 300,
                RequestTimeoutSeconds = 10
            }
        });

    private static PracticeCompletionRequest CreateCompletionRequest() =>
        new(
            "session-key",
            100,
            new PracticeCompletionData(3, Guid.NewGuid()),
            DateTimeOffset.Parse("2026-09-06T10:40:00Z"));

    private static string CreateEventMessageJson(
        Guid sessionId,
        string submittedSql,
        string sessionKey = "session-key") =>
        JsonSerializer.Serialize(
            new PracticeEventMessage(
                sessionId,
                sessionKey,
                Guid.NewGuid(),
                "AttemptSubmitted",
                DateTimeOffset.Parse("2026-09-06T10:40:00Z"),
                new PracticeEventPayload(
                    submittedSql,
                    "Rejected",
                    null,
                    null,
                    false,
                    "test")),
            PlatformIntegrationJson.Default);

    private static IOptions<EducationClientOptions> CreateEducationClientOptions() =>
        Options.Create(new EducationClientOptions
        {
            ServiceKey = ServiceKey,
            EducationBaseUrl = "http://education.test",
        });

    private sealed class RecordingEventPublisher(
        string failingMarker,
        string failureMessage = "Simulated broker failure") : IPracticeEventPublisher
    {
        internal List<Guid> PublishedIds { get; } = [];

        public Task PublishAsync(PracticeEventMessage message, CancellationToken ct = default)
        {
            var messageJson = JsonSerializer.Serialize(message, PlatformIntegrationJson.Default);
            if (messageJson.Contains(failingMarker, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(failureMessage);
            }

            PublishedIds.Add(message.SessionId);
            return Task.CompletedTask;
        }
    }

    private sealed class StubCompletionClient(
        EducationCompletionDeliveryResult result = EducationCompletionDeliveryResult.Accepted,
        bool shouldThrow = false) : IEducationCompletionClient
    {
        internal int Calls { get; private set; }

        public Task<EducationCompletionDeliveryResult> CompleteAsync(
            Guid sessionId,
            PracticeCompletionRequest request,
            CancellationToken ct = default)
        {
            Calls++;
            return shouldThrow
                ? Task.FromException<EducationCompletionDeliveryResult>(
                    new HttpRequestException("Simulated Education outage"))
                : Task.FromResult(result);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        internal List<string> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            RecordingScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(formatter(state, exception) + exception?.ToString());

        private sealed class RecordingScope : IDisposable
        {
            internal static readonly RecordingScope Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset current = utcNow;

        public override DateTimeOffset GetUtcNow() => current;

        internal void Advance(TimeSpan value) => current = current.Add(value);
    }

    private sealed class StaticResponseHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        internal string? RequestPath { get; private set; }
        internal string? ServiceKey { get; private set; }
        internal string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath;
            ServiceKey = request.Headers.GetValues("X-Service-Key").Single();
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }

    private async Task<(Guid Published, Guid Draft, Guid Archived)> SeedTasksAsync(
        string marker,
        string publishedText)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DbmsDictionary.Create(
            $"Catalog {marker}", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var targetDb = TargetDb.Create(dbms.Id, $"catalog_{marker}", null, false);
        var topic = Topic.Create($"Catalog {marker}");
        var publishedQuery = SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        var draftQuery = SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        var archivedQuery = SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        var published = SqlTask.Create(
            topic.Id, publishedQuery.Id, $"A Published {marker}", publishedText, 1,
            publicationStatus: PublicationStatus.Published);
        var draft = SqlTask.Create(
            topic.Id, draftQuery.Id, $"B Draft {marker}", "draft", 1);
        var archived = SqlTask.Create(
            topic.Id, archivedQuery.Id, $"C Archived {marker}", "archived", 1,
            publicationStatus: PublicationStatus.Archived);
        db.AddRange(dbms, targetDb, topic, publishedQuery, draftQuery, archivedQuery, published, draft, archived);
        await db.SaveChangesAsync();
        return (published.Id, draft.Id, archived.Id);
    }
}
