using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.Infrastructure;
using DomainAttempt = SQLModule.Domain.Training.Attempt;

namespace SQLModule.IntegrationTests.Training.SqlTask;

/// <summary>Публикация SQL-задания требует активной версии оценки; причины блокировки и защита учебной базы.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SqlTaskPublishReadinessTests(TestApplication app) : ApiTestBase(app)
{
    private sealed record DraftTask(Guid TaskId, Guid TargetDbId, Guid TopicId);

    private async Task<DraftTask> CreateDraftTaskAsync()
    {
        AsAdmin();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dbms = await DbmsDictionaryClient.CreateAsync(new CreateDbmsDictionaryRequest(
            $"Readiness_{suffix}", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password"));
        AsTeacher();
        var targetDb = await TargetDbClient.CreateAsync(new CreateTargetDbRequest(
            dbms.Id, $"readiness_{suffix}", null, false));
        var topic = await TopicClient.CreateAsync(new CreateTopicRequest($"Readiness_{suffix}", null));
        var task = await SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
            topic.Id, $"Readiness task {suffix}", "Проверить запрос", 2,
            new ReferenceQueryRequest(targetDb.Id, "SELECT 1", false, false)));
        return new DraftTask(task.Id, targetDb.Id, topic.Id);
    }

    private async Task<TaskValidationConfigurationResponse> GetValidationAsync(Guid taskId) =>
        (await HttpClient.GetFromJsonAsync<TaskValidationConfigurationResponse>(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId), ClientJson.Options))!;

    private async Task<HttpResponseMessage> PostPublishValidationAsync(
        Guid taskId, string version, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post, ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId))
        {
            Content = JsonContent.Create(new PublishTaskValidationRequest(version), options: ClientJson.Options)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await HttpClient.SendAsync(request);
    }

    [Fact(DisplayName = "Publish без версии оценки → 409 SqlTask.ValidationVersionNotPublished, задание остаётся Draft")]
    public async Task Publish_WithoutValidationVersion_IsRejectedAndKeepsDraft()
    {
        var draft = await CreateDraftTaskAsync();

        using var response = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForPublish(draft.TaskId), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(409);
        body.RootElement.GetProperty("code").GetString().ShouldBe("SqlTask.ValidationVersionNotPublished");
        body.RootElement.GetProperty("detail").GetString().ShouldNotBeNullOrWhiteSpace();
        (await SqlTaskClient.GetByIdAsync(draft.TaskId))!.PublicationStatus.ShouldBe(PublicationStatus.Draft);
    }

    [Fact(DisplayName = "Publish после публикации версии оценки → задание опубликовано")]
    public async Task Publish_AfterValidationVersionPublished_Succeeds()
    {
        var draft = await CreateDraftTaskAsync();
        await PublishValidationAsync(draft.TaskId);

        var published = await SqlTaskClient.PublishAsync(draft.TaskId);

        published.PublicationStatus.ShouldBe(PublicationStatus.Published);
    }

    [Fact(DisplayName = "Publish задания с несколькими причинами → в errors все причины, code — первая")]
    public async Task Publish_WithSeveralBlockers_ReportsAllInErrors()
    {
        var taskId = await CreateDomainTaskWithoutValidationAsync(PublicationStatus.Draft);
        await SeedAttemptAsync(taskId);

        using var response = await HttpClient.PostAsync(ApiRoutes.Training.SqlTasks.ForPublish(taskId), null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().ShouldBe("SqlTask.ValidationVersionNotPublished");
        body.RootElement.GetProperty("errors").EnumerateObject().Select(pair => pair.Name).ToArray().ShouldBe(
        [
            "SqlTask.ValidationVersionNotPublished",
            "SqlTask.ReferenceQueryNotValidated",
            "SqlTask.HasAttemptsOnPublish"
        ]);
    }

    [Fact(DisplayName = "Teacher details: все причины блокировки в стабильном порядке, canPublish = false")]
    public async Task TeacherDetails_ListsAllBlockersInStableOrder()
    {
        var taskId = await CreateDomainTaskWithoutValidationAsync(PublicationStatus.Draft);
        await SeedAttemptAsync(taskId);

        var first = await SqlTaskClient.GetTeacherDetailsAsync(taskId);
        var second = await SqlTaskClient.GetTeacherDetailsAsync(taskId);

        first.ShouldNotBeNull();
        first.CanPublish.ShouldBeFalse();
        first.PublishBlockers.Select(blocker => blocker.Code).ToArray().ShouldBe(
        [
            "SqlTask.ValidationVersionNotPublished",
            "SqlTask.ReferenceQueryNotValidated",
            "SqlTask.HasAttemptsOnPublish"
        ]);
        first.PublishBlockers.ShouldAllBe(blocker =>
            !String.IsNullOrWhiteSpace(blocker.Code) && !String.IsNullOrWhiteSpace(blocker.Message));
        second!.PublishBlockers.Select(blocker => blocker.Code)
            .ShouldBe(first.PublishBlockers.Select(blocker => blocker.Code));
    }

    [Fact(DisplayName = "Teacher details: готовое задание — publishBlockers пуст, canPublish = true")]
    public async Task TeacherDetails_ReadyTask_HasNoBlockers()
    {
        var draft = await CreateDraftTaskAsync();
        (await SqlTaskClient.GetTeacherDetailsAsync(draft.TaskId))!.PublishBlockers
            .Select(blocker => blocker.Code).ShouldBe(["SqlTask.ValidationVersionNotPublished"]);

        await PublishValidationAsync(draft.TaskId);
        var details = await SqlTaskClient.GetTeacherDetailsAsync(draft.TaskId);

        details!.PublishBlockers.ShouldBeEmpty();
        details.CanPublish.ShouldBeTrue();
    }

    [Fact(DisplayName = "Уже опубликованное задание без версии оценки: читается, есть блокировка, canPublish = false")]
    public async Task PublishedTaskWithoutVersion_ReportsBlocker()
    {
        var taskId = await CreateDomainTaskWithoutValidationAsync(PublicationStatus.Published);

        var details = await SqlTaskClient.GetTeacherDetailsAsync(taskId);

        details.ShouldNotBeNull();
        details.PublishBlockers.Select(blocker => blocker.Code).ShouldContain("SqlTask.ValidationVersionNotPublished");
        details.CanPublish.ShouldBeFalse();
    }

    [Fact(DisplayName = "Схема изменилась после публикации оценки → результат эталона устарел, повторная публикация оценки снимает блокировку")]
    public async Task SchemaChangeAfterValidation_MarksReferenceStaleUntilRepublished()
    {
        var draft = await CreateDraftTaskAsync();
        await PublishValidationAsync(draft.TaskId);
        var firstVersion = await GetValidationAsync(draft.TaskId);

        await MetaTableClient.CreateAsync(new CreateMetaTableRequest(draft.TargetDbId, "late_table", null));
        var stale = await SqlTaskClient.GetTeacherDetailsAsync(draft.TaskId);

        stale!.CanPublish.ShouldBeFalse();
        stale.PublishBlockers.Select(blocker => blocker.Code).ShouldBe(["SqlTask.ReferenceQueryNotValidated"]);

        await PublishValidationAsync(draft.TaskId);
        var refreshed = await GetValidationAsync(draft.TaskId);
        var ready = await SqlTaskClient.GetTeacherDetailsAsync(draft.TaskId);

        refreshed.ValidationVersionNumber.ShouldBe(firstVersion.ValidationVersionNumber + 1);
        refreshed.HasUnpublishedChanges.ShouldBeFalse();
        ready!.PublishBlockers.ShouldBeEmpty();
        ready.CanPublish.ShouldBeTrue();
    }

    [Fact(DisplayName = "Семантика оценки: версии нет / версия совпадает с черновиком / черновик изменён")]
    public async Task ValidationConfiguration_HasUnpublishedChanges_Semantics()
    {
        var draft = await CreateDraftTaskAsync();

        var none = await GetValidationAsync(draft.TaskId);
        none.ValidationVersionId.ShouldBeNull();
        none.HasUnpublishedChanges.ShouldBeFalse();
        none.State.ShouldBe(ValidationConfigurationState.Draft);

        await PublishValidationAsync(draft.TaskId);
        var published = await GetValidationAsync(draft.TaskId);
        published.ValidationVersionId.ShouldNotBeNull();
        published.HasUnpublishedChanges.ShouldBeFalse();
        published.State.ShouldBe(ValidationConfigurationState.Published);

        var update = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(draft.TaskId),
            new TaskValidationConfigurationRequest(
                published.Version, 90, null, [HintGroup.Result],
                [new ValidationCheckRequest(
                    published.Checks[0].Id, ValidationCheckKind.MainDatasetResult, null, 100, 0)]),
            ClientJson.Options);
        update.StatusCode.ShouldBe(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        var changed = await GetValidationAsync(draft.TaskId);
        changed.ValidationVersionId.ShouldBe(published.ValidationVersionId);
        changed.HasUnpublishedChanges.ShouldBeTrue();
        changed.State.ShouldBe(ValidationConfigurationState.Draft);
    }

    [Fact(DisplayName = "Публикация оценки идемпотентна: тот же Idempotency-Key возвращает исходную версию")]
    public async Task PublishValidation_SameKey_ReturnsSameVersion()
    {
        var draft = await CreateDraftTaskAsync();
        var configuration = await GetValidationAsync(draft.TaskId);
        var key = Guid.NewGuid().ToString("D");

        using var first = await PostPublishValidationAsync(draft.TaskId, configuration.Version, key);
        using var replay = await PostPublishValidationAsync(draft.TaskId, configuration.Version, key);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstBody = (await first.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options))!;
        var replayBody = (await replay.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options))!;
        replayBody.ValidationVersionId.ShouldBe(firstBody.ValidationVersionId);
        replayBody.ValidationVersionNumber.ShouldBe(firstBody.ValidationVersionNumber);
        firstBody.HasUnpublishedChanges.ShouldBeFalse();
    }

    [Fact(DisplayName = "Публикация оценки не публикует задание")]
    public async Task PublishValidation_DoesNotPublishTask()
    {
        var draft = await CreateDraftTaskAsync();

        await PublishValidationAsync(draft.TaskId);

        (await SqlTaskClient.GetByIdAsync(draft.TaskId))!.PublicationStatus.ShouldBe(PublicationStatus.Draft);
    }

    [Fact(DisplayName = "Новая версия оценки не меняет версию уже начатого прохождения")]
    public async Task NewValidationVersion_DoesNotChangeActiveProgress()
    {
        var draft = await CreateDraftTaskAsync();
        await PublishValidationAsync(draft.TaskId);
        await SqlTaskClient.PublishAsync(draft.TaskId);
        AsStudent(Guid.NewGuid());
        using (var start = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Student.ForTaskProgress(draft.TaskId)))
        {
            start.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            (await HttpClient.SendAsync(start)).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        AsTeacher();
        var configuration = await GetValidationAsync(draft.TaskId);
        (await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(draft.TaskId),
            new TaskValidationConfigurationRequest(
                configuration.Version, 80, null, [HintGroup.Result],
                [new ValidationCheckRequest(
                    configuration.Checks[0].Id, ValidationCheckKind.MainDatasetResult, null, 100, 0)]),
            ClientJson.Options)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await PublishValidationAsync(draft.TaskId);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versions = await db.TaskValidationVersions.AsNoTracking()
            .Where(value => value.TaskId == draft.TaskId).OrderBy(value => value.VersionNumber)
            .Select(value => value.Id).ToListAsync();
        var progress = await db.StudentTaskProgresses.AsNoTracking().SingleAsync(value => value.TaskId == draft.TaskId);
        versions.Count.ShouldBe(2);
        progress.ValidationVersionId.ShouldBe(versions[0]);
        (await db.SqlTasks.AsNoTracking().SingleAsync(value => value.Id == draft.TaskId))
            .ActiveValidationVersionId.ShouldBe(versions[1]);
    }

    [Fact(DisplayName = "Учебная база опубликованного задания защищена: изменение схемы → 409 TargetDb.PublishedTaskReferenceExists")]
    public async Task PublishedTaskTargetDb_RejectsSchemaChanges()
    {
        var draft = await CreateDraftTaskAsync();
        var table = await MetaTableClient.CreateAsync(new CreateMetaTableRequest(draft.TargetDbId, "before_publish", null));
        await PublishValidationAsync(draft.TaskId);
        await SqlTaskClient.PublishAsync(draft.TaskId);

        var create = await Should.ThrowAsync<ConflictException>(() =>
            MetaTableClient.CreateAsync(new CreateMetaTableRequest(draft.TargetDbId, "after_publish", null)));
        var delete = await Should.ThrowAsync<ConflictException>(() => MetaTableClient.DeleteAsync(table.Id));

        create.Problem!.Code.ShouldBe("TargetDb.PublishedTaskReferenceExists");
        create.Problem.Errors!["taskIds"].ShouldBe([draft.TaskId.ToString("D")]);
        delete.Problem!.Code.ShouldBe("TargetDb.PublishedTaskReferenceExists");
        (await MetaTableClient.GetByIdAsync(table.Id)).ShouldNotBeNull();
    }

    [Fact(DisplayName = "После архивации задания учебную базу снова можно менять")]
    public async Task ArchivedTaskReleasesTargetDb()
    {
        var draft = await CreateDraftTaskAsync();
        await PublishValidationAsync(draft.TaskId);
        await SqlTaskClient.PublishAsync(draft.TaskId);
        await Should.ThrowAsync<ConflictException>(() =>
            MetaTableClient.CreateAsync(new CreateMetaTableRequest(draft.TargetDbId, "blocked", null)));

        await SqlTaskClient.ArchiveAsync(draft.TaskId);

        (await MetaTableClient.CreateAsync(new CreateMetaTableRequest(draft.TargetDbId, "allowed", null)))
            .TableName.ShouldBe("allowed");
    }

    [Fact(DisplayName = "Учебная база черновика не защищена")]
    public async Task DraftTaskTargetDb_IsEditable()
    {
        var draft = await CreateDraftTaskAsync();

        (await MetaTableClient.CreateAsync(new CreateMetaTableRequest(draft.TargetDbId, "free", null)))
            .TableName.ShouldBe("free");
    }

    private async Task<Guid> CreateDomainTaskWithoutValidationAsync(PublicationStatus status)
    {
        var draft = await CreateDraftTaskAsync();
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = Domain.Training.SqlQuery.Create("SELECT 1", false, false, draft.TargetDbId);
        var task = Domain.Training.SqlTask.Create(
            draft.TopicId, query.Id, "Domain task without validation", "Text", 1, publicationStatus: status);
        db.SqlQueries.Add(query);
        db.SqlTasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    private async Task SeedAttemptAsync(Guid taskId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Attempts.Add(DomainAttempt.Record(
            Guid.NewGuid(), taskId, "SELECT 1",
            ExecutionStatus.Succeeded, true, CheckReason.Ok,
            0, 0, null,
            DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }
}
