using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Student;
using SQLModule.Data.Core;
using SQLModule.Host;
using SQLModule.IntegrationTests.Infrastructure;
using SQLModule.IntegrationTests.ModuleIntegration;

namespace SQLModule.IntegrationTests.Training.Student;

/// <summary>Проверяет правила доступа студента к открытому банку: видимость, платформенные токены, разбор.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TaskBankAccessTests : ApiTestBase
{
    public TaskBankAccessTests(TestApplication app)
        : base(app)
    {
    }

    [Fact(DisplayName = "Банк: контрольное задание даёт 404 на каждой студенческой ручке и не создаёт попыток")]
    public async Task ControlTask_IsNotFound_OnEveryStudentRoute()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var userId = Guid.NewGuid();
        AsStudent(userId);

        var responses = new Dictionary<string, HttpStatusCode>
        {
            ["details"] = (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTask(taskId))).StatusCode,
            ["schema"] = (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskSchema(taskId))).StatusCode,
            ["rows"] = (await HttpClient.GetAsync(
                ApiRoutes.Training.Student.ForTaskTableRows(taskId, Guid.NewGuid(), 0, 10))).StatusCode,
            ["hint"] = (await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskHint(taskId), null)).StatusCode,
            ["solution"] = (await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId), null)).StatusCode,
            ["by-number"] = (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskByBankNumber(long.MaxValue))).StatusCode
        };
        using var submitRequest = PlatformProfileAndCatalogTests.CreateSubmitRequest(taskId, userId, null);
        responses["submit"] = (await HttpClient.SendAsync(submitRequest)).StatusCode;

        responses.ShouldAllBe(pair => pair.Value == HttpStatusCode.NotFound, string.Join(", ", responses));
        var catalog = await HttpClient.GetFromJsonAsync<PageResponse<StudentTaskResponse>>(
            ApiRoutes.Training.Student.Tasks, ClientJson.Options);
        catalog!.Items.ShouldNotContain(item => item.Id == taskId);
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.UserId == userId)).ShouldBe(0);
    }

    [Fact(DisplayName = "Банк: закрытое задание скрывает тренировку, но своя история попыток остаётся")]
    public async Task ClosedTask_HidesTrainingRoutes_ButKeepsOwnHistory()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var state = await OpenAsync(taskId);
        var userId = Guid.NewGuid();
        var attemptId = await TaskBankTestData.SeedAttemptAsync(
            App, taskId, userId, 1, 0, false, state.BankPublishedAt!.Value.AddSeconds(1));
        AsTeacher();
        using var close = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankClose(taskId), null);
        close.StatusCode.ShouldBe(HttpStatusCode.OK);
        AsStudent(userId);

        using var details = await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTask(taskId));
        using var hint = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskHint(taskId), null);
        using var solution = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId), null);
        using var submitRequest = PlatformProfileAndCatalogTests.CreateSubmitRequest(taskId, userId, null);
        using var submit = await HttpClient.SendAsync(submitRequest);
        var history = await HttpClient.GetFromJsonAsync<PageResponse<StudentAttemptListItemResponse>>(
            ApiRoutes.Training.Student.Attempts, ClientJson.Options);
        using var attempt = await HttpClient.GetAsync(ApiRoutes.Training.Student.ForAttempt(attemptId));

        details.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        hint.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        solution.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        submit.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        history!.Items.ShouldContain(item => item.Id == attemptId);
        attempt.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Банк: платформенный токен не получает каталог, подсказку и разбор даже у открытого задания")]
    public async Task PlatformSession_CannotUseBankOnlyEndpoints_EvenForOpenTask()
    {
        using var platform = CreatePlatformApplication();
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(platform);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        await PlatformProfileAndCatalogTests.SeedModuleSessionAsync(platform, sessionId, userId, taskId, null);
        using var client = platform.CreateClient();
        var state = await OpenAsync(client, taskId);

        var statuses = new Dictionary<string, HttpStatusCode>
        {
            ["topics"] = await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.Topics, userId, sessionId),
            ["tasks"] = await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.Tasks, userId, sessionId),
            ["by-number"] = await StatusAsync(
                client, HttpMethod.Get, ApiRoutes.Training.Student.ForTaskByBankNumber(state.BankNumber!.Value), userId, sessionId),
            ["hint"] = await StatusAsync(
                client, HttpMethod.Post, ApiRoutes.Training.Student.ForTaskHint(taskId), userId, sessionId),
            ["solution"] = await StatusAsync(
                client, HttpMethod.Post, ApiRoutes.Training.Student.ForTaskSolution(taskId), userId, sessionId),
            ["history"] = await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.Attempts, userId, sessionId)
        };

        statuses.ShouldAllBe(pair => pair.Value == HttpStatusCode.Forbidden, string.Join(", ", statuses));
        (await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(taskId), userId, sessionId))
            .ShouldBe(HttpStatusCode.OK);
        using var scope = platform.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.BankHintAccesses.CountAsync(value => value.UserId == userId)).ShouldBe(0);
        (await db.BankSolutionAccesses.CountAsync(value => value.UserId == userId)).ShouldBe(0);
    }

    [Fact(DisplayName = "Банк: контрольное задание остаётся доступным своей платформенной сессии")]
    public async Task ControlTask_RemainsAvailableToItsPlatformSession()
    {
        using var platform = CreatePlatformApplication();
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(platform);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        await PlatformProfileAndCatalogTests.SeedModuleSessionAsync(platform, sessionId, userId, taskId, null);
        using var client = platform.CreateClient();

        var details = await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(taskId), userId, sessionId);
        var schema = await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.ForTaskSchema(taskId), userId, sessionId);
        var withoutSession = await StatusAsync(client, HttpMethod.Get, ApiRoutes.Training.Student.ForTask(taskId), userId, null);

        details.ShouldBe(HttpStatusCode.OK);
        schema.ShouldBe(HttpStatusCode.OK);
        withoutSession.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Банк: ShowSolution=false закрывает разбор сразу и без записи в журнал, повторное включение открывает")]
    public async Task ShowSolutionDisabled_RejectsSolution_AndWritesNoAccess()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var state = await OpenAsync(taskId);
        var userId = Guid.NewGuid();
        await TaskBankTestData.SeedAttemptAsync(
            App, taskId, userId, 1, 100, true, state.BankPublishedAt!.Value.AddSeconds(1));

        await SetShowSolutionAsync(taskId, false);
        AsStudent(userId);
        using var disabled = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId), null);
        disabled.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await disabled.Content.ReadAsStringAsync()).ShouldContain("TaskBank.SolutionDisabled");
        (await CountSolutionAccessesAsync(userId, taskId)).ShouldBe(0);

        await SetShowSolutionAsync(taskId, true);
        AsStudent(userId);
        using var enabled = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId), null);
        enabled.StatusCode.ShouldBe(HttpStatusCode.OK, await enabled.Content.ReadAsStringAsync());
        (await CountSolutionAccessesAsync(userId, taskId)).ShouldBe(1);

        await SetShowSolutionAsync(taskId, false);
        AsStudent(userId);
        using var disabledAgain = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId), null);
        disabledAgain.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CountSolutionAccessesAsync(userId, taskId)).ShouldBe(1);
    }

    [Fact(DisplayName = "Банк: попытки до открытия в банк не дают «решено», подсказку и разбор, не попадают в статистику")]
    public async Task AttemptsBeforeBankPublication_DoNotCount()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var state = await OpenAsync(taskId);
        var publishedAt = state.BankPublishedAt!.Value;
        var userId = Guid.NewGuid();
        await TaskBankTestData.SeedAttemptAsync(App, taskId, userId, 1, 100, true, publishedAt.AddMinutes(-5));
        await TaskBankTestData.SeedAttemptAsync(App, taskId, userId, 2, 0, false, publishedAt.AddMinutes(-4));
        int outboxBefore;
        using (var scope = App.Services.CreateScope())
        {
            outboxBefore = await scope.ServiceProvider.GetRequiredService<AppDbContext>().PendingPublishes.CountAsync();
        }

        AsStudent(userId);
        using var hintBefore = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskHint(taskId), null);
        using var solutionBefore = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId), null);
        var catalogBefore = await HttpClient.GetFromJsonAsync<PageResponse<StudentTaskResponse>>(
            ApiRoutes.Training.Student.Tasks, ClientJson.Options);
        var itemBefore = catalogBefore!.Items.Single(item => item.Id == taskId);

        hintBefore.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await hintBefore.Content.ReadAsStringAsync()).ShouldContain("TaskBank.HintAttemptRequired");
        solutionBefore.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await solutionBefore.Content.ReadAsStringAsync()).ShouldContain("TaskBank.SolutionAttemptRequired");
        itemBefore.Status.ShouldBe(BankTaskStatus.NotStarted);
        itemBefore.AttemptsCount.ShouldBe(0);

        await TaskBankTestData.SeedAttemptAsync(App, taskId, userId, 3, 0, false, publishedAt.AddSeconds(1));
        using var hintAfter = await HttpClient.PostAsync(ApiRoutes.Training.Student.ForTaskHint(taskId), null);
        var catalogAfter = await HttpClient.GetFromJsonAsync<PageResponse<StudentTaskResponse>>(
            ApiRoutes.Training.Student.Tasks, ClientJson.Options);
        var itemAfter = catalogAfter!.Items.Single(item => item.Id == taskId);

        hintAfter.StatusCode.ShouldBe(HttpStatusCode.OK, await hintAfter.Content.ReadAsStringAsync());
        itemAfter.Status.ShouldBe(BankTaskStatus.InProgress);
        itemAfter.AttemptsCount.ShouldBe(1);
        using var verificationScope = App.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.PendingPublishes.CountAsync()).ShouldBe(outboxBefore);
    }

    private WebApplicationFactory<IHostMarker> CreatePlatformApplication() =>
        App.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Platform");
            builder.ConfigureAppConfiguration((_, configuration) =>
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

    private async Task<TaskBankStateResponse> OpenAsync(Guid taskId)
    {
        AsTeacher();
        using var response = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TaskBankStateResponse>(ClientJson.Options))!;
    }

    private static async Task<TaskBankStateResponse> OpenAsync(HttpClient client, Guid taskId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.SqlTasks.ForBankOpen(taskId));
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        request.Headers.Add("X-Test-Roles", "Teacher");
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TaskBankStateResponse>(ClientJson.Options))!;
    }

    private static async Task<HttpStatusCode> StatusAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        Guid userId,
        Guid? sessionId)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Headers.Add("X-Test-Roles", "Student");
        if (sessionId.HasValue)
        {
            request.Headers.Add("X-Test-SessionId", sessionId.Value.ToString());
        }

        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task SetShowSolutionAsync(Guid taskId, bool showSolution)
    {
        AsTeacher();
        using var response = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForBankContent(taskId),
            new UpdateTaskBankContentRequest(ShowSolution: showSolution),
            ClientJson.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<int> CountSolutionAccessesAsync(Guid userId, Guid taskId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.BankSolutionAccesses.CountAsync(value => value.UserId == userId && value.TaskId == taskId);
    }
}
