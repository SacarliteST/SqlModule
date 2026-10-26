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
using SQLModule.IntegrationTests.Training.Student;

namespace SQLModule.IntegrationTests.Training.SqlTask;

/// <summary>Проверяет защитные правила управления открытым банком: права, подсказки, конкурентность, ограничения БД.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TaskBankGuardTests : ApiTestBase
{
    public TaskBankGuardTests(TestApplication app)
        : base(app)
    {
    }

    [Fact(DisplayName = "Банк: роль Student получает 403 на всех teacher-операциях банка")]
    public async Task StudentRole_CannotUseTeacherBankEndpoints()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        AsStudent();

        var statuses = new Dictionary<string, HttpStatusCode>
        {
            ["readiness"] = (await HttpClient.GetAsync(ApiRoutes.Training.SqlTasks.ForBankReadiness(taskId))).StatusCode,
            ["open"] = (await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null)).StatusCode,
            ["close"] = (await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankClose(taskId), null)).StatusCode,
            ["content"] = (await HttpClient.PutAsJsonAsync(
                ApiRoutes.Training.SqlTasks.ForBankContent(taskId),
                new UpdateTaskBankContentRequest("Подсказка", true, null),
                ClientJson.Options)).StatusCode,
            ["batch"] = (await HttpClient.PostAsJsonAsync(
                ApiRoutes.Training.SqlTasks.BankBatch,
                new UpdateTaskBankBatchRequest([taskId], true),
                ClientJson.Options)).StatusCode
        };

        statuses.ShouldAllBe(pair => pair.Value == HttpStatusCode.Forbidden, string.Join(", ", statuses));
        await AssertTaskStateAsync(taskId, Domain.Training.TaskVisibility.Control, null);
    }

    [Fact(DisplayName = "Банк: последнюю подсказку у открытого задания удалить нельзя, пока есть другая — можно")]
    public async Task RemovingLastHint_OfOpenTask_IsRejected()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        await DisableAutomaticHintsAsync(taskId);

        using var openWithoutHint = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null);
        openWithoutHint.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await openWithoutHint.Content.ReadAsStringAsync()).ShouldContain("TaskBank.HintMissing");
        await AssertTaskStateAsync(taskId, Domain.Training.TaskVisibility.Control, null);

        (await UpdateContentAsync(taskId, "Первая подсказка")).StatusCode.ShouldBe(HttpStatusCode.OK);
        using var open = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForBankOpen(taskId), null);
        open.StatusCode.ShouldBe(HttpStatusCode.OK, await open.Content.ReadAsStringAsync());

        using var removeLast = await UpdateContentAsync(taskId, null);
        removeLast.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await removeLast.Content.ReadAsStringAsync()).ShouldContain("TaskBank.HintMissing");
        (await ReadBankHintAsync(taskId)).ShouldBe("Первая подсказка");

        (await UpdateContentAsync(taskId, "Вторая подсказка")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadBankHintAsync(taskId)).ShouldBe("Вторая подсказка");
    }

    [Fact(DisplayName = "Банк: пакетное открытие частично успешно и возвращает причины по пропущенным заданиям")]
    public async Task Batch_OpensReadyTasks_AndReportsBlockersForOthers()
    {
        var readyId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var draft = await CreateDraftTaskAsync();

        using var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.SqlTasks.BankBatch,
            new UpdateTaskBankBatchRequest([readyId, draft.Id], true),
            ClientJson.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var batch = (await response.Content.ReadFromJsonAsync<TaskBankBatchResponse>(ClientJson.Options))!;
        batch.UpdatedCount.ShouldBe(1);
        batch.SkippedCount.ShouldBe(1);
        batch.Items.Single(item => item.TaskId == readyId).Updated.ShouldBeTrue();
        var skipped = batch.Items.Single(item => item.TaskId == draft.Id);
        skipped.Updated.ShouldBeFalse();
        skipped.Blockers.ShouldContain(blocker => blocker.Code == "TaskBank.NotPublished");
        await AssertTaskStateAsync(readyId, Domain.Training.TaskVisibility.Open, expectNumber: true);
        await AssertTaskStateAsync(draft.Id, Domain.Training.TaskVisibility.Control, null);
    }

    [Fact(DisplayName = "Банк: устаревшее состояние задания отклоняется токенами конкурентности")]
    public async Task ConcurrencyTokens_RejectStaleChanges()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        using var firstScope = App.Services.CreateScope();
        using var secondScope = App.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var second = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staleForOpen = await first.SqlTasks.SingleAsync(value => value.Id == taskId);
        var competing = await second.SqlTasks.SingleAsync(value => value.Id == taskId);
        competing.UpdateBankContent("Конкурирующая подсказка", true, null);
        await second.SaveChangesAsync();
        staleForOpen.OpenInBank(long.MaxValue - 1, DateTimeOffset.UtcNow);
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => first.SaveChangesAsync());

        first.ChangeTracker.Clear();
        second.ChangeTracker.Clear();
        var staleForContent = await first.SqlTasks.SingleAsync(value => value.Id == taskId);
        var opener = await second.SqlTasks.SingleAsync(value => value.Id == taskId);
        opener.OpenInBank(long.MaxValue - 2, DateTimeOffset.UtcNow);
        await second.SaveChangesAsync();
        staleForContent.UpdateBankContent("Устаревшая правка", false, null);
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => first.SaveChangesAsync());

        first.ChangeTracker.Clear();
        var final = await first.SqlTasks.AsNoTracking().SingleAsync(value => value.Id == taskId);
        final.Visibility.ShouldBe(Domain.Training.TaskVisibility.Open);
        final.BankHint.ShouldBe("Конкурирующая подсказка");
        final.ShowSolution.ShouldBeTrue();
    }

    [Fact(DisplayName = "Банк: ограничение БД не допускает открытое задание без номера и даты публикации")]
    public async Task DatabaseConstraint_RejectsOpenTaskWithoutBankNumber()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exception = await Should.ThrowAsync<Exception>(() => db.Database.ExecuteSqlRawAsync(
            "UPDATE \"SqlTasks\" SET \"Visibility\" = 'Open' WHERE \"Id\" = {0}", taskId));

        exception.ToString().ShouldContain("CK_SqlTasks_OpenBankState");
    }

    [Fact(DisplayName = "Банк: тренировочную попытку удалить нельзя, платформенную можно")]
    public async Task DeleteAttempt_RejectsTrainingAttempts()
    {
        var taskId = await PlatformProfileAndCatalogTests.SeedValidatedRunnableTaskAsync(App);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ModuleSessions.Add(ModuleSession.Create(
                sessionId, "delete-attempt-session", userId, taskId.ToString(), "https://platform.example/return", null));
            await db.SaveChangesAsync();
        }

        var trainingId = await TaskBankTestData.SeedAttemptAsync(App, taskId, userId, 1, 0, false, DateTimeOffset.UtcNow);
        var platformId = await TaskBankTestData.SeedAttemptAsync(
            App, taskId, userId, 1, 0, false, DateTimeOffset.UtcNow, sessionId);
        AsAdmin();

        using var training = await HttpClient.DeleteAsync(ApiRoutes.Training.Attempts.ForId(trainingId));
        using var platform = await HttpClient.DeleteAsync(ApiRoutes.Training.Attempts.ForId(platformId));

        training.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await training.Content.ReadAsStringAsync()).ShouldContain("Attempt.TrainingDeletionForbidden");
        platform.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var verification = App.Services.CreateScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verificationDb.Attempts.AnyAsync(value => value.Id == trainingId)).ShouldBeTrue();
        (await verificationDb.Attempts.AnyAsync(value => value.Id == platformId)).ShouldBeFalse();
    }

    private async Task<HttpResponseMessage> UpdateContentAsync(Guid taskId, string? hint)
    {
        AsTeacher();
        return await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForBankContent(taskId),
            new UpdateTaskBankContentRequest(hint, true, null),
            ClientJson.Options);
    }

    private async Task DisableAutomaticHintsAsync(Guid taskId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"TaskValidationVersions\" SET \"VisibleHintGroupsMask\" = 0 WHERE \"TaskId\" = {0}", taskId);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"TaskValidationConfigurations\" SET \"VisibleHintGroupsMask\" = 0 WHERE \"TaskId\" = {0}", taskId);
    }

    private async Task<string?> ReadBankHintAsync(Guid taskId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId)
            .Select(value => value.BankHint)
            .SingleAsync();
    }

    private async Task AssertTaskStateAsync(
        Guid taskId,
        Domain.Training.TaskVisibility visibility,
        bool? expectNumber)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = await db.SqlTasks.AsNoTracking().SingleAsync(value => value.Id == taskId);
        task.Visibility.ShouldBe(visibility);
        if (expectNumber == true)
        {
            task.BankNumber.ShouldNotBeNull();
        }
        else
        {
            task.BankNumber.ShouldBeNull();
        }
    }

    private async Task<SqlTaskResponse> CreateDraftTaskAsync()
    {
        AsAdmin();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var dbms = await DbmsDictionaryClient.CreateAsync(
            new Contracts.DbmsCatalog.DbmsDictionary.CreateDbmsDictionaryRequest(
                $"Guard {marker}", "postgres", "postgres:latest", 5432,
                "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
                "training", "user", "password"));
        AsTeacher();
        var targetDb = await TargetDbClient.CreateAsync(
            new Contracts.Schema.TargetDb.CreateTargetDbRequest(
                dbms.Id, $"guard_{marker}", null, false));
        var topic = await TopicClient.CreateAsync(
            new Contracts.Training.Topic.CreateTopicRequest($"Guard {marker}", null));
        return await SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
            topic.Id,
            $"Guard {marker}",
            "Условие",
            1,
            new ReferenceQueryRequest(targetDb.Id, "SELECT 1", false, false)));
    }
}
