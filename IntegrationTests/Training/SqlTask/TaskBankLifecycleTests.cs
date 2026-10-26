using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.IntegrationTests.Infrastructure;
using SQLModule.IntegrationTests.ModuleIntegration;

namespace SQLModule.IntegrationTests.Training.SqlTask;

/// <summary>Проверяет жизненный цикл задания открытого банка.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TaskBankLifecycleTests : ApiTestBase
{
    public TaskBankLifecycleTests(TestApplication app)
        : base(app)
    {
    }

    [Fact(DisplayName = "Банк: открытие назначает постоянный номер, закрытие его сохраняет")]
    public async Task OpenCloseReopen_PreservesNumberAndFirstPublicationDate()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);

        using var firstOpen = await HttpClient.PostAsync(
            ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null);
        firstOpen.StatusCode.ShouldBe(HttpStatusCode.OK, await firstOpen.Content.ReadAsStringAsync());
        var firstState = (await firstOpen.Content.ReadFromJsonAsync<TaskBankStateResponse>(ClientJson.Options))!;

        using var close = await HttpClient.PostAsync(
            ApiRoutes.Training.SqlTasks.ForBankClose(taskId), null);
        close.StatusCode.ShouldBe(HttpStatusCode.OK, await close.Content.ReadAsStringAsync());
        var closedState = (await close.Content.ReadFromJsonAsync<TaskBankStateResponse>(ClientJson.Options))!;

        using var secondOpen = await HttpClient.PostAsync(
            ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null);
        secondOpen.StatusCode.ShouldBe(HttpStatusCode.OK, await secondOpen.Content.ReadAsStringAsync());
        var secondState = (await secondOpen.Content.ReadFromJsonAsync<TaskBankStateResponse>(ClientJson.Options))!;

        firstState.Visibility.ShouldBe(Contracts.Training.TaskVisibility.Open);
        firstState.BankNumber.ShouldNotBeNull();
        firstState.BankPublishedAt.ShouldNotBeNull();
        closedState.Visibility.ShouldBe(Contracts.Training.TaskVisibility.Control);
        closedState.BankNumber.ShouldBe(firstState.BankNumber);
        secondState.BankNumber.ShouldBe(firstState.BankNumber);
        secondState.BankPublishedAt.ShouldBe(firstState.BankPublishedAt);
    }

    [Fact(DisplayName = "Банк: готовность сообщает контрольное использование")]
    public async Task Readiness_ReturnsUsedInControl()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ModuleSessions.Add(ModuleSession.Create(
                Guid.NewGuid(),
                "bank-readiness-session",
                Guid.NewGuid(),
                taskId.ToString(),
                "https://platform.example/return",
                null));
            await db.SaveChangesAsync();
        }

        var readiness = await HttpClient.GetFromJsonAsync<TaskBankReadinessResponse>(
            ApiRoutes.Training.SqlTasks.ForBankReadiness(taskId),
            ClientJson.Options);

        readiness!.CanOpen.ShouldBeTrue();
        readiness.UsedInControl.ShouldBeTrue();
        readiness.Blockers.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Банк: неготовое задание возвращает все причины и остаётся закрытым")]
    public async Task OpenUnreadyTask_ReturnsAllBlockers()
    {
        var draft = await CreateDraftTaskAsync();

        using var response = await HttpClient.PostAsync(
            ApiRoutes.Training.SqlTasks.ForBankOpen(draft.Id), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("TaskBank.NotPublished");
        body.ShouldContain("SqlTask.ValidationVersionNotPublished");
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = await db.SqlTasks.AsNoTracking().SingleAsync(value => value.Id == draft.Id);
        task.Visibility.ShouldBe(Domain.Training.TaskVisibility.Control);
        task.BankNumber.ShouldBeNull();
    }

    private async Task<SqlTaskResponse> CreateDraftTaskAsync()
    {
        AsAdmin();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var dbms = await DbmsDictionaryClient.CreateAsync(
            new Contracts.DbmsCatalog.DbmsDictionary.CreateDbmsDictionaryRequest(
                $"Bank {marker}", "postgres", "postgres:latest", 5432,
                "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
                "training", "user", "password"));
        AsTeacher();
        var targetDb = await TargetDbClient.CreateAsync(
            new Contracts.Schema.TargetDb.CreateTargetDbRequest(
                dbms.Id, $"bank_{marker}", null, false));
        var topic = await TopicClient.CreateAsync(
            new Contracts.Training.Topic.CreateTopicRequest($"Bank {marker}", null));
        return await SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
            topic.Id,
            $"Bank {marker}",
            "Условие",
            1,
            new ReferenceQueryRequest(targetDb.Id, "SELECT 1", false, false)));
    }
}
