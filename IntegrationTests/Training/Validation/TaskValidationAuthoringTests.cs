using System.Net;
using System.Net.Http.Json;
using EducationSPD.Kafka.Contracts;
using EducationSPD.Kafka.Contracts.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Application.Training.Episodes;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.Episode;
using SQLModule.Contracts.Training.Skill;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.Infrastructure;

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
        response.State.ShouldBe(SQLModule.Contracts.Training.ValidationConfigurationState.Draft);
        response.ValidationVersionId.ShouldBeNull();
        response.HasUnpublishedChanges.ShouldBeFalse();
        response.PassingScore.ShouldBe(100);
        response.VisibleHintGroups.ShouldBe([SQLModule.Contracts.Training.HintGroup.Result]);
        var check = response.Checks.ShouldHaveSingleItem();
        check.Kind.ShouldBe(SQLModule.Contracts.Training.ValidationCheckKind.MainDatasetResult);
        check.Weight.ShouldBe(100);
        check.Skill.ShouldNotBeNull();
        check.Skill.SkillId.ShouldBe("validation.maindatasetresult.main-dataset-result");
        check.Skill.Origin.ShouldBe(SkillOriginResponse.System);
        response.SkillTags.ShouldBeEmpty();
        response.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
    }

    [Fact(DisplayName = "Validation authoring: преподаватель назначает только навыки-теги")]
    public async Task Put_AssignsTeacherTagAndRejectsDeletedTag()
    {
        var taskId = await CreateTaskAsync("SELECT DISTINCT 1");
        var initial = await GetConfigurationAsync(taskId);
        var tag = await CreateSkillAsync();
        var request = ValidDistinctConfiguration(initial, [tag.Id]);

        using var updateResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            request,
            ClientJson.Options);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(
            ClientJson.Options);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());
        updated.ShouldNotBeNull();
        updated.SkillTags.ShouldHaveSingleItem().Id.ShouldBe(tag.Id);
        updated.SkillTagsWarning.ShouldNotBeNullOrWhiteSpace();
        updated.Checks.ShouldAllBe(check => check.Skill != null);

        using var deleteResponse = await HttpClient.DeleteAsync(ApiRoutes.Training.Skills.ForId(tag.Id));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var rejectedResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            ValidDistinctConfiguration(updated, [tag.Id]),
            ClientJson.Options);

        rejectedResponse.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await rejectedResponse.Content.ReadAsStringAsync()).ShouldContain("Validation.SkillUnavailable");
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
        var request = new TaskValidationConfigurationRequest(null, null, null, null);

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
            [SQLModule.Contracts.Training.HintGroup.Result, SQLModule.Contracts.Training.HintGroup.RequiredConstructs],
            [
                new ValidationCheckPreviewRequest(
                    before.Checks[0].Id, null, SQLModule.Contracts.Training.ValidationCheckKind.MainDatasetResult, null, 70, 0),
                new ValidationCheckPreviewRequest(
                    null, "new-distinct", SQLModule.Contracts.Training.ValidationCheckKind.RequiredConstruct, "Distinct", 30, 1)
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
            .ShouldBe(SQLModule.Contracts.Training.ValidationCheckStatus.Passed);
        after.Version.ShouldBe(before.Version);
        after.Checks.ShouldHaveSingleItem();
    }

    [Fact(DisplayName = "Validation authoring: publish создаёт immutable snapshot и повтор выполняется идемпотентно")]
    public async Task Publish_CreatesSingleImmutableVersion()
    {
        var taskId = await CreateTaskAsync("SELECT DISTINCT 1");
        var initial = await GetConfigurationAsync(taskId);
        var tag = await CreateSkillAsync();
        var update = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            ValidDistinctConfiguration(initial, [tag.Id]),
            ClientJson.Options);
        update.StatusCode.ShouldBe(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        var draft = (await update.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(
            ClientJson.Options))!;
        var publishRequest = new PublishTaskValidationRequest(draft.Version);
        var idempotencyKey = Guid.NewGuid().ToString("D");

        using var firstRequest = new HttpRequestMessage(
            HttpMethod.Post, ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId))
        {
            Content = JsonContent.Create(publishRequest, options: ClientJson.Options)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var first = await HttpClient.SendAsync(firstRequest);
        var firstBody = await first.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options);
        using var secondRequest = new HttpRequestMessage(
            HttpMethod.Post, ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId))
        {
            Content = JsonContent.Create(publishRequest, options: ClientJson.Options)
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        var second = await HttpClient.SendAsync(secondRequest);
        var secondBody = await second.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options);

        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstBody.ShouldNotBeNull();
        secondBody.ShouldNotBeNull();
        firstBody.State.ShouldBe(SQLModule.Contracts.Training.ValidationConfigurationState.Published);
        firstBody.HasUnpublishedChanges.ShouldBeFalse();
        firstBody.ValidationVersionNumber.ShouldBe(1);
        secondBody.ValidationVersionId.ShouldBe(firstBody.ValidationVersionId);

        var nextDraftResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            new TaskValidationConfigurationRequest(
                firstBody.Version,
                100,
                [SQLModule.Contracts.Training.HintGroup.Result],
                [new ValidationCheckRequest(
                    firstBody.Checks[0].Id,
                    SQLModule.Contracts.Training.ValidationCheckKind.MainDatasetResult,
                    null,
                    100,
                    0)]),
            ClientJson.Options);
        nextDraftResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await nextDraftResponse.Content.ReadAsStringAsync());

        using var renameResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.Skills.ForId(tag.Id),
            new UpdateSkillRequest("Переименованный навык"),
            ClientJson.Options);
        renameResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

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
        version.ValidationConfigurationSnapshotJson.ShouldContain(
            "validation.requiredconstruct.distinct");
        version.ValidationConfigurationSnapshotJson.ShouldContain("Проектирование запроса");
        version.ValidationConfigurationSnapshotJson.ShouldNotContain("Переименованный навык");
    }

    [Fact(DisplayName = "Learning episode: повторный старт возобновляет эпизод, отказ финализирует его один раз")]
    public async Task Episode_StartResumeAndAbandon_IsIdempotent()
    {
        var taskId = await CreateTaskAsync("SELECT 1");
        var tag = await CreateSkillAsync();
        await PublishWithTagAsync(taskId, tag.Id);
        AsStudent();

        using var startResponse = await HttpClient.PostAsync(
            ApiRoutes.Training.Student.ForTaskEpisodes(taskId), null);
        var started = await startResponse.Content.ReadFromJsonAsync<LearningEpisodeResponse>(ClientJson.Options);
        using var resumeResponse = await HttpClient.PostAsync(
            ApiRoutes.Training.Student.ForTaskEpisodes(taskId), null);
        var resumed = await resumeResponse.Content.ReadFromJsonAsync<LearningEpisodeResponse>(ClientJson.Options);

        startResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        resumeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        started.ShouldNotBeNull();
        resumed!.EpisodeId.ShouldBe(started.EpisodeId);

        var key = Guid.NewGuid().ToString("D");
        using var firstFinalize = CreateFinalizeEpisodeRequest(started.EpisodeId, key);
        using var firstResponse = await HttpClient.SendAsync(firstFinalize);
        using var repeatedFinalize = CreateFinalizeEpisodeRequest(started.EpisodeId, key);
        using var repeatedResponse = await HttpClient.SendAsync(repeatedFinalize);

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await firstResponse.Content.ReadAsStringAsync());
        repeatedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var finalized = await firstResponse.Content.ReadFromJsonAsync<LearningEpisodeResponse>(ClientJson.Options);
        finalized!.Status.ShouldBe(LearningEpisodeStatusResponse.Finalized);
        finalized.ProducesObservation.ShouldBeTrue();

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var episode = await db.LearningEpisodes.AsNoTracking()
            .SingleAsync(value => value.Id == started.EpisodeId);
        episode.FinalizationReason.ShouldBe(LearningEpisodeFinalizationReason.Abandoned);
        episode.FinalAttemptId.ShouldBeNull();
        var result = await db.LearningEpisodeSkillResults.AsNoTracking()
            .SingleAsync(value => value.EpisodeId == episode.Id);
        result.SkillId.ShouldBe(tag.Id);
        result.Source.ShouldBe(LearningEpisodeSkillSource.TaskTag);
        result.Outcome.ShouldBe(LearningEpisodeSkillOutcome.Failed);
        var outbox = await db.PendingPublishes.AsNoTracking()
            .SingleAsync(value =>
                value.Kind == PendingPublishKind.LearningEpisode &&
                value.SessionId == episode.Id);
        outbox.Topic.ShouldBe("learning.episode-finalized");
        outbox.MessageKey.ShouldBe($"sql:{episode.StudentId:D}");
        var message = KafkaContractJson.Deserialize<LearningEpisodeFinalized>(outbox.MessageJson);
        message.EventId.ShouldBe(outbox.Id);
        message.EpisodeId.ShouldBe(episode.Id);
        message.FinalizedAt.ToUnixTimeMilliseconds().ShouldBe(
            episode.FinalizedAt!.Value.ToUnixTimeMilliseconds());
        message.SkillResults.ShouldHaveSingleItem().Outcome.ShouldBe(SkillOutcome.Failed);
    }

    [Fact(DisplayName = "Learning episode: отсутствие heartbeat больше трёх минут финализируется один раз")]
    public async Task Episode_HeartbeatTimeout_FinalizesOnce()
    {
        var taskId = await CreateTaskAsync("SELECT 1");
        var tag = await CreateSkillAsync();
        await PublishWithTagAsync(taskId, tag.Id);
        AsStudent();
        using var startResponse = await HttpClient.PostAsync(
            ApiRoutes.Training.Student.ForTaskEpisodes(taskId), null);
        var started = (await startResponse.Content.ReadFromJsonAsync<LearningEpisodeResponse>(
            ClientJson.Options))!;

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.LearningEpisodes.Where(value => value.Id == started.EpisodeId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                value => value.LastHeartbeatAt,
                DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(4))));
        var processor = scope.ServiceProvider.GetRequiredService<LearningEpisodeTimeoutProcessor>();

        (await processor.ProcessAsync(CancellationToken.None)).ShouldBe(1);
        (await processor.ProcessAsync(CancellationToken.None)).ShouldBe(0);

        var episode = await db.LearningEpisodes.AsNoTracking()
            .SingleAsync(value => value.Id == started.EpisodeId);
        episode.Status.ShouldBe(LearningEpisodeStatus.Finalized);
        episode.FinalizationReason.ShouldBe(LearningEpisodeFinalizationReason.HeartbeatTimeout);
        (await db.LearningEpisodeSkillResults.CountAsync(value => value.EpisodeId == episode.Id))
            .ShouldBe(1);
        (await db.PendingPublishes.CountAsync(value =>
            value.Kind == PendingPublishKind.LearningEpisode &&
            value.SessionId == episode.Id)).ShouldBe(1);
    }

    [Fact(DisplayName = "Learning episode: отказ без попытки и тегов не создаёт непроверяемое событие")]
    public async Task Episode_AbandonWithoutAttemptOrTags_DoesNotCreateOutboxEvent()
    {
        var taskId = await CreateTaskAsync("SELECT 1");
        var current = await GetConfigurationAsync(taskId);
        using var publishRequest = new HttpRequestMessage(
            HttpMethod.Post,
            ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId))
        {
            Content = JsonContent.Create(
                new PublishTaskValidationRequest(current.Version),
                options: ClientJson.Options)
        };
        publishRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        using var publishResponse = await HttpClient.SendAsync(publishRequest);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await publishResponse.Content.ReadAsStringAsync());
        await SqlTaskClient.PublishAsync(taskId);
        AsStudent();
        using var startResponse = await HttpClient.PostAsync(
            ApiRoutes.Training.Student.ForTaskEpisodes(taskId), null);
        var started = (await startResponse.Content.ReadFromJsonAsync<LearningEpisodeResponse>(
            ClientJson.Options))!;
        using var finalizeRequest = CreateFinalizeEpisodeRequest(
            started.EpisodeId,
            Guid.NewGuid().ToString("D"));
        using var finalizeResponse = await HttpClient.SendAsync(finalizeRequest);
        finalizeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.PendingPublishes.CountAsync(value =>
            value.Kind == PendingPublishKind.LearningEpisode &&
            value.SessionId == started.EpisodeId)).ShouldBe(0);
    }

    private async Task<TaskValidationConfigurationResponse> GetConfigurationAsync(Guid taskId) =>
        (await HttpClient.GetFromJsonAsync<TaskValidationConfigurationResponse>(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            ClientJson.Options))!;

    private static TaskValidationConfigurationRequest ValidDistinctConfiguration(
        TaskValidationConfigurationResponse current,
        IReadOnlyList<Guid>? skillTagIds = null) =>
        new(
            current.Version,
            71,
            [SQLModule.Contracts.Training.HintGroup.Result, SQLModule.Contracts.Training.HintGroup.RequiredConstructs],
            [
                new ValidationCheckRequest(
                    current.Checks[0].Id, SQLModule.Contracts.Training.ValidationCheckKind.MainDatasetResult, null, 70, 0),
                new ValidationCheckRequest(
                    null, SQLModule.Contracts.Training.ValidationCheckKind.RequiredConstruct, "Distinct", 30, 1)
            ],
            skillTagIds);

    private async Task<SkillResponse> CreateSkillAsync()
    {
        using var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.Skills.Collection,
            new CreateSkillRequest($"validation-tag-{Guid.NewGuid():N}", "Проектирование запроса"),
            ClientJson.Options);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SkillResponse>(ClientJson.Options))!;
    }

    private async Task PublishWithTagAsync(Guid taskId, Guid tagId)
    {
        var current = await GetConfigurationAsync(taskId);
        using var updateResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(taskId),
            new TaskValidationConfigurationRequest(
                current.Version,
                100,
                [SQLModule.Contracts.Training.HintGroup.Result],
                [new ValidationCheckRequest(
                    current.Checks[0].Id,
                    SQLModule.Contracts.Training.ValidationCheckKind.MainDatasetResult,
                    null,
                    100,
                    0)],
                [tagId]),
            ClientJson.Options);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(
            ClientJson.Options))!;
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());

        using var publishRequest = new HttpRequestMessage(
            HttpMethod.Post,
            ApiRoutes.Training.SqlTasks.ForValidationPublish(taskId))
        {
            Content = JsonContent.Create(
                new PublishTaskValidationRequest(updated.Version),
                options: ClientJson.Options)
        };
        publishRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        using var publishResponse = await HttpClient.SendAsync(publishRequest);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await publishResponse.Content.ReadAsStringAsync());
        await SqlTaskClient.PublishAsync(taskId);
    }

    private static HttpRequestMessage CreateFinalizeEpisodeRequest(Guid episodeId, string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            ApiRoutes.Training.Student.ForEpisodeFinalize(episodeId))
        {
            Content = JsonContent.Create(
                new FinalizeLearningEpisodeRequest(LearningEpisodeFinalizationMode.Abandon),
                options: ClientJson.Options)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return request;
    }

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
