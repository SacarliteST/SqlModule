using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.Infrastructure;
using SQLModule.IntegrationTests.ModuleIntegration;

namespace SQLModule.IntegrationTests.Training.Student;

/// <summary>Проверяет студенческие сценарии открытого банка.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TaskBankStudentApiTests : ApiTestBase
{
    public TaskBankStudentApiTests(TestApplication app)
        : base(app)
    {
    }

    [Fact(DisplayName = "Банк: каталог показывает только открытое задание и поддерживает номер")]
    public async Task Catalog_ReturnsOnlyOpenTaskAndSupportsBankNumber()
    {
        var openTaskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var controlTaskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var state = await OpenAsync(openTaskId);
        var userId = Guid.NewGuid();
        AsStudent(userId);

        var catalog = await HttpClient.GetFromJsonAsync<PageResponse<StudentTaskResponse>>(
            ApiRoutes.Training.Student.Tasks,
            ClientJson.Options);

        catalog!.Items.ShouldContain(item => item.Id == openTaskId && item.BankNumber == state.BankNumber);
        catalog.Items.ShouldNotContain(item => item.Id == controlTaskId);
        using var byNumber = await HttpClient.GetAsync(
            ApiRoutes.Training.Student.ForTaskByBankNumber(state.BankNumber!.Value));
        byNumber.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await byNumber.Content.ReadAsStringAsync();
        body.ShouldNotContain("sqlText", Case.Insensitive);
        body.ShouldNotContain("hints", Case.Insensitive);
    }

    [Fact(DisplayName = "Банк: подсказка и разбор открываются только после нужных попыток и журналируются")]
    public async Task HintAndSolution_RequireAttemptsAndWriteAccessLogs()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var state = await OpenAsync(taskId);
        var userId = Guid.NewGuid();
        AsStudent(userId);

        using var earlyHint = await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskHint(taskId));
        using var earlySolution = await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId));
        earlyHint.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        earlySolution.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        await SeedAttemptAsync(taskId, userId, 1, 0, false, state.BankPublishedAt!.Value.AddSeconds(1));
        using var hint = await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskHint(taskId));
        hint.StatusCode.ShouldBe(HttpStatusCode.OK, await hint.Content.ReadAsStringAsync());
        var hintResponse = (await hint.Content.ReadFromJsonAsync<BankHintResponse>(ClientJson.Options))!;
        hintResponse.AttemptNumber.ShouldBe(1);

        await SeedAttemptAsync(taskId, userId, 2, 100, true, state.BankPublishedAt.Value.AddSeconds(2));
        using var solution = await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTaskSolution(taskId));
        solution.StatusCode.ShouldBe(HttpStatusCode.OK, await solution.Content.ReadAsStringAsync());
        var solutionResponse = (await solution.Content.ReadFromJsonAsync<BankSolutionResponse>(ClientJson.Options))!;
        solutionResponse.SqlText.ShouldNotBeNullOrWhiteSpace();

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.BankHintAccesses.CountAsync(value => value.UserId == userId && value.TaskId == taskId)).ShouldBe(1);
        (await db.BankSolutionAccesses.CountAsync(value => value.UserId == userId && value.TaskId == taskId)).ShouldBe(1);
    }

    [Fact(DisplayName = "Банк: идемпотентный повтор обходит лимитер, новая частая отправка получает 429")]
    public async Task SubmitReplay_BypassesRateLimitButNewRequestIsRejected()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        await OpenAsync(taskId);
        var userId = Guid.NewGuid();
        var key = Guid.NewGuid().ToString("D");
        int outboxBefore;
        using (var scope = App.Services.CreateScope())
        {
            outboxBefore = await scope.ServiceProvider.GetRequiredService<AppDbContext>()
                .PendingPublishes.CountAsync();
        }

        using var firstRequest = PlatformProfileAndCatalogTests.CreateSubmitRequest(taskId, userId, null, key);
        using var first = await HttpClient.SendAsync(firstRequest);
        using var replayRequest = PlatformProfileAndCatalogTests.CreateSubmitRequest(taskId, userId, null, key);
        using var replay = await HttpClient.SendAsync(replayRequest);
        using var frequentRequest = PlatformProfileAndCatalogTests.CreateSubmitRequest(taskId, userId, null);
        using var frequent = await HttpClient.SendAsync(frequentRequest);

        first.StatusCode.ShouldBe(HttpStatusCode.Created, await first.Content.ReadAsStringAsync());
        replay.StatusCode.ShouldBe(HttpStatusCode.Created, await replay.Content.ReadAsStringAsync());
        frequent.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests, await frequent.Content.ReadAsStringAsync());
        frequent.Headers.RetryAfter.ShouldNotBeNull();
        using var verificationScope = App.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(value => value.UserId == userId && value.TaskId == taskId)).ShouldBe(1);
        (await db.PendingPublishes.CountAsync()).ShouldBe(outboxBefore);
    }

    private async Task<TaskBankStateResponse> OpenAsync(Guid taskId)
    {
        AsTeacher();
        using var response = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TaskBankStateResponse>(ClientJson.Options))!;
    }

    private async Task SeedAttemptAsync(
        Guid taskId,
        Guid userId,
        int number,
        int score,
        bool correct,
        DateTimeOffset finishedAt)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var version = await db.TaskValidationVersions.SingleAsync(value => value.TaskId == taskId);
        var check = await db.ValidationChecks.OrderBy(value => value.Order)
            .FirstAsync(value => value.Configuration.TaskId == taskId);
        var attempt = SQLModule.Domain.Training.Attempt.Record(
            userId,
            taskId,
            "SELECT 1",
            ExecutionStatus.Succeeded,
            correct,
            correct ? CheckReason.Ok : CheckReason.ValueMismatch,
            1,
            1,
            null,
            finishedAt.AddMilliseconds(-1),
            finishedAt,
            studentName: "Bank Student");
        attempt.AttachScoring(version.Id, number, score, true);
        db.Attempts.Add(attempt);
        db.AttemptCheckResults.Add(AttemptCheckResult.Create(
            attempt.Id,
            check.Id,
            check.Kind,
            correct ? ValidationCheckStatus.Passed : ValidationCheckStatus.Failed,
            check.Weight,
            score,
            check.Order,
            null,
            null));
        await db.SaveChangesAsync();
    }
}
