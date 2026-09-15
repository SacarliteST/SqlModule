using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.Validation;

[Collection(IntegrationTestCollection.Name)]
public sealed class TaskValidationAuthoringTests(TestApplication app) : ApiTestBase(app)
{
    [Fact(DisplayName = "Validation authoring: GET возвращает созданную вместе с заданием конфигурацию по умолчанию")]
    public async Task Get_ReturnsDefaultConfiguration()
    {
        var taskId = await CreateTaskAsync("SELECT 1");

        var response = await GetConfigurationAsync(taskId);

        response.TaskId.ShouldBe(taskId);
        response.State.ShouldBe(ValidationConfigurationState.Draft);
        response.HasUnpublishedChanges.ShouldBeTrue();
        response.PassingScore.ShouldBe(100);
        response.MaxAttempts.ShouldBeNull();
        response.VisibleHintGroups.ShouldBe([HintGroup.Result]);
        var check = response.Checks.ShouldHaveSingleItem();
        check.Kind.ShouldBe(ValidationCheckKind.MainDatasetResult);
        check.Weight.ShouldBe(100);
        response.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
    }

    [Fact(DisplayName = "Validation authoring: PUT сохраняет полный draft и stale version возвращает 409")]
    public async Task Put_UpdatesDraftAndRejectsStaleVersion()
    {
        var taskId = await CreateTaskAsync("SELECT DISTINCT 1");
        var initial = await GetConfigurationAsync(taskId);
        var request = ValidDistinctConfiguration(initial);

        var updateResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            request,
            ClientJson.Options);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(
            ClientJson.Options);
        var staleResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            request,
            ClientJson.Options);

        updateResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await updateResponse.Content.ReadAsStringAsync());
        updated.ShouldNotBeNull();
        updated.Version.ShouldNotBe(initial.Version);
        updated.PassingScore.ShouldBe(71);
        updated.Checks.Count.ShouldBe(2);
        staleResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Validation authoring: nullable PUT-контракт возвращает структурированный 422")]
    public async Task Put_MissingRequiredFieldsReturnsValidationProblem()
    {
        var taskId = await CreateTaskAsync("SELECT 1");
        var request = new TaskValidationConfigurationRequest(null, null, null, null, null);

        var response = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            request,
            ClientJson.Options);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        body.ShouldContain("Version");
        body.ShouldContain("PassingScore");
        body.ShouldContain("VisibleHintGroups");
        body.ShouldContain("Checks");
    }

    [Fact(DisplayName = "Validation authoring: конфигурация не сохраняется, если эталон не набирает 100 баллов")]
    public async Task Put_ReferenceBelowHundredDoesNotMutateDraft()
    {
        var taskId = await CreateTaskAsync("SELECT 1");
        var before = await GetConfigurationAsync(taskId);

        var response = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            ValidDistinctConfiguration(before),
            ClientJson.Options);
        var body = await response.Content.ReadAsStringAsync();
        var after = await GetConfigurationAsync(taskId);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        body.ShouldContain("Validation.ReferenceMustScore100");
        after.Version.ShouldBe(before.Version);
        after.Checks.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Validation authoring: preview сохраняет clientKey и не изменяет конфигурацию")]
    public async Task Preview_ReturnsCheckBreakdownWithoutMutation()
    {
        var taskId = await CreateTaskAsync("SELECT DISTINCT 1");
        var before = await GetConfigurationAsync(taskId);
        var previewRequest = new TaskValidationPreviewRequest(
            71,
            5,
            [HintGroup.Result, HintGroup.RequiredConstructs],
            [
                new ValidationCheckPreviewRequest(
                    before.Checks[0].Id, null, ValidationCheckKind.MainDatasetResult, null, 70, 0),
                new ValidationCheckPreviewRequest(
                    null, "new-distinct", ValidationCheckKind.RequiredConstruct, "Distinct", 30, 1)
            ]);

        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidationPreview(taskId),
            previewRequest,
            ClientJson.Options);
        var preview = await response.Content.ReadFromJsonAsync<TaskValidationPreviewResponse>(ClientJson.Options);
        var after = await GetConfigurationAsync(taskId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        preview.ShouldNotBeNull();
        preview.IsValid.ShouldBeTrue();
        preview.ReferenceScore.ShouldBe(100);
        preview.Checks.Single(check => check.ClientKey == "new-distinct").Status
            .ShouldBe(ValidationCheckStatus.Passed);
        after.Version.ShouldBe(before.Version);
        after.Checks.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Validation authoring: publish создаёт immutable snapshot и повтор выполняется идемпотентно")]
    public async Task Publish_CreatesSingleImmutableVersion()
    {
        var taskId = await CreateTaskAsync("SELECT DISTINCT 1");
        var initial = await GetConfigurationAsync(taskId);
        var update = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            ValidDistinctConfiguration(initial),
            ClientJson.Options);
        update.StatusCode.ShouldBe(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        var draft = (await update.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(
            ClientJson.Options))!;
        var publishRequest = new PublishTaskValidationRequest(draft.Version);

        var first = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId),
            publishRequest,
            ClientJson.Options);
        var firstBody = await first.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options);
        var second = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId),
            publishRequest,
            ClientJson.Options);
        var secondBody = await second.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options);

        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstBody.ShouldNotBeNull();
        secondBody.ShouldNotBeNull();
        firstBody.State.ShouldBe(ValidationConfigurationState.Published);
        firstBody.HasUnpublishedChanges.ShouldBeFalse();
        firstBody.ValidationVersionNumber.ShouldBe(1);
        secondBody.ValidationVersionId.ShouldBe(firstBody.ValidationVersionId);

        var nextDraftResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            new TaskValidationConfigurationRequest(
                firstBody.Version,
                100,
                null,
                [HintGroup.Result],
                [new ValidationCheckRequest(
                    firstBody.Checks[0].Id,
                    ValidationCheckKind.MainDatasetResult,
                    null,
                    100,
                    0)]),
            ClientJson.Options);
        nextDraftResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await nextDraftResponse.Content.ReadAsStringAsync());

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versions = await db.TaskValidationVersions.AsNoTracking()
            .Where(version => version.TaskId == taskId)
            .ToArrayAsync();
        var version = versions.ShouldHaveSingleItem();
        version.SchemaSnapshotJson.ShouldNotBeNullOrWhiteSpace();
        version.DatasetSnapshotJson.ShouldNotBeNullOrWhiteSpace();
        version.ReferenceQuerySnapshotJson.ShouldContain("SELECT DISTINCT 1");
        version.ExpectedResultSnapshotJson.ShouldNotBeNullOrWhiteSpace();
        version.ValidationConfigurationSnapshotJson.ShouldContain("Distinct");
    }

    private async Task<TaskValidationConfigurationResponse> GetConfigurationAsync(Guid taskId) =>
        (await HttpClient.GetFromJsonAsync<TaskValidationConfigurationResponse>(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            ClientJson.Options))!;

    private static TaskValidationConfigurationRequest ValidDistinctConfiguration(
        TaskValidationConfigurationResponse current) =>
        new(
            current.Version,
            71,
            5,
            [HintGroup.Result, HintGroup.RequiredConstructs],
            [
                new ValidationCheckRequest(
                    current.Checks[0].Id, ValidationCheckKind.MainDatasetResult, null, 70, 0),
                new ValidationCheckRequest(
                    null, ValidationCheckKind.RequiredConstruct, "Distinct", 30, 1)
            ]);

    private async Task<Guid> CreateTaskAsync(string referenceSql)
    {
        AsAdmin();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dbms = await DbmsDictionaryClient.CreateAsync(new CreateDbmsDictionaryRequest(
            $"Validation_{suffix}", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password"));
        AsTeacher();
        var targetDb = await TargetDbClient.CreateAsync(new CreateTargetDbRequest(
            dbms.Id, $"validation_{suffix}", null, false));
        var topic = await TopicClient.CreateAsync(new CreateTopicRequest($"Validation_{suffix}", null));
        var task = await SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
            topic.Id,
            $"Validation task {suffix}",
            "Проверить запрос",
            2,
            new ReferenceQueryRequest(targetDb.Id, referenceSql, false, false)));
        return task.Id;
    }
}
