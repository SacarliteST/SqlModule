using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.Training.Progress;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Isolated;

namespace SQLModule.IntegrationTests.Training.Validation;

[Collection(IntegrationTestCollection.Name)]
public sealed class StudentProgressTests(TestApplication app) : ApiTestBase(app)
{
    [Fact(DisplayName = "Progress: start идемпотентен и закрепляет опубликованную версию")]
    public async Task Start_IsIdempotentAndPinsVersion()
    {
        var taskId = await CreatePublishedTaskAsync(1);
        var studentId = Guid.NewGuid();
        AsStudent(studentId);
        var key = Guid.NewGuid();

        var first = await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), key);
        var second = await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), key);
        var firstBody = await first.Content.ReadFromJsonAsync<StudentTaskProgressResponse>(ClientJson.Options);
        var secondBody = await second.Content.ReadFromJsonAsync<StudentTaskProgressResponse>(ClientJson.Options);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstBody.ShouldNotBeNull();
        secondBody!.Id.ShouldBe(firstBody.Id);
        firstBody.AttemptsRemaining.ShouldBe(1);
        firstBody.CanSubmit.ShouldBeTrue();

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var progress = await db.StudentTaskProgresses.AsNoTracking().SingleAsync(value => value.Id == firstBody.Id);
        var activeVersionId = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId)
            .Select(value => value.ActiveValidationVersionId)
            .SingleAsync();
        progress.ValidationVersionId.ShouldBe(activeVersionId!.Value);
    }

    [Fact(DisplayName = "Progress: restart запрещён пока прохождение можно продолжать")]
    public async Task Restart_RejectsActiveProgress()
    {
        var taskId = await CreatePublishedTaskAsync(1);
        AsStudent(Guid.NewGuid());
        await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), Guid.NewGuid());

        var response = await PostAsync(
            ApiRoutes.Training.Student.ForTaskProgressRestart(taskId), Guid.NewGuid());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Progress.StillActive");
    }

    [Fact(DisplayName = "Progress: restart сохраняет старое прохождение и создаёт новое")]
    public async Task Restart_AfterLimitCreatesNewProgress()
    {
        var taskId = await CreatePublishedTaskAsync(1);
        AsStudent(Guid.NewGuid());
        var started = await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), Guid.NewGuid());
        var oldProgress = (await started.Content.ReadFromJsonAsync<StudentTaskProgressResponse>(ClientJson.Options))!;

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.StudentTaskProgresses.Where(value => value.Id == oldProgress.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.AttemptsUsed, 1));
        }

        var response = await PostAsync(
            ApiRoutes.Training.Student.ForTaskProgressRestart(taskId), Guid.NewGuid());
        var restarted = await response.Content.ReadFromJsonAsync<StudentTaskProgressResponse>(ClientJson.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        restarted.ShouldNotBeNull();
        restarted.Id.ShouldNotBe(oldProgress.Id);
        restarted.AttemptsUsed.ShouldBe(0);

        using var verificationScope = App.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var oldState = await verificationDb.StudentTaskProgresses.AsNoTracking()
            .SingleAsync(value => value.Id == oldProgress.Id);
        oldState.Status.ShouldBe(ProgressStatus.Completed);
        oldState.FinalizationReason.ShouldBe(FinalizationReason.Restarted);
    }

    [Fact(DisplayName = "Reservation: параллельные запросы не превышают maxAttempts")]
    public async Task Reservation_ConcurrentRequestsRespectLimit()
    {
        var taskId = await CreatePublishedTaskAsync(1);
        AsStudent(Guid.NewGuid());
        var started = await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), Guid.NewGuid());
        var progress = (await started.Content.ReadFromJsonAsync<StudentTaskProgressResponse>(ClientJson.Options))!;

        using var scope1 = App.Services.CreateScope();
        using var scope2 = App.Services.CreateScope();
        var service1 = scope1.ServiceProvider.GetRequiredService<IAttemptReservationService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<IAttemptReservationService>();

        var results = await Task.WhenAll(
            service1.ReserveAsync(progress.Id, Guid.NewGuid(), new string('A', 64), CancellationToken.None),
            service2.ReserveAsync(progress.Id, Guid.NewGuid(), new string('B', 64), CancellationToken.None));

        results.Count(result => result.IsSuccess).ShouldBe(1);
        results.Count(result => result.Error?.Code == "Progress.AttemptsExhausted").ShouldBe(1);
    }

    [Fact(DisplayName = "Progress: platform session создаёт одно прохождение и закрепляет версию")]
    public async Task PlatformSession_CreatesSinglePinnedProgress()
    {
        var taskId = await CreatePublishedTaskAsync(null);
        var studentId = Guid.NewGuid();
        var session = ModuleSession.Create(
            Guid.NewGuid(), "session-key", studentId, taskId.ToString("D"),
            "https://education.test/return", DateTimeOffset.UtcNow.AddHours(1));

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IPlatformProgressService>();
        db.ModuleSessions.Add(session);

        var first = await service.EnsureCreatedAsync(session, CancellationToken.None);
        await db.SaveChangesAsync();
        var second = await service.EnsureCreatedAsync(session, CancellationToken.None);
        await db.SaveChangesAsync();

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        second.Value!.Id.ShouldBe(first.Value!.Id);
        first.Value.ModuleSessionId.ShouldBe(session.Id);
        first.Value.UserId.ShouldBe(studentId);
        var activeVersionId = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId)
            .Select(value => value.ActiveValidationVersionId)
            .SingleAsync();
        first.Value.ValidationVersionId.ShouldBe(activeVersionId!.Value);
    }

    [Fact(DisplayName = "Scoring: попытка сохраняет score, breakdown и обновляет progress идемпотентно")]
    public async Task Submit_SavesCompositeScoreAndUpdatesProgress()
    {
        var taskId = await CreatePublishedTaskAsync(2);
        AsStudent(Guid.NewGuid());
        await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), Guid.NewGuid());
        var executor = App.Services.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<FakeSandboxExecutor>();
        executor.OverrideRun = Result<QueryResultSet>.Success(new QueryResultSet(true, null, [], [], 0, 1));
        executor.ResetRunCallCount();
        var key = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(new SubmitAttemptRequest(taskId, "SELECT 1"), options: ClientJson.Options)
        };
        request.Headers.Add("Idempotency-Key", key.ToString("D"));
        using var response = await HttpClient.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<SubmitAttemptResponse>(ClientJson.Options);

        using var replayRequest = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(new SubmitAttemptRequest(taskId, "SELECT 1"), options: ClientJson.Options)
        };
        replayRequest.Headers.Add("Idempotency-Key", key.ToString("D"));
        using var replay = await HttpClient.SendAsync(replayRequest);
        var replayBody = await replay.Content.ReadFromJsonAsync<SubmitAttemptResponse>(ClientJson.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        replay.StatusCode.ShouldBe(HttpStatusCode.Created, await replay.Content.ReadAsStringAsync());
        body.ShouldNotBeNull();
        body.Score.ShouldBe(100);
        body.BestScore.ShouldBe(100);
        body.AttemptNumber.ShouldBe(1);
        body.AttemptsUsed.ShouldBe(1);
        body.Checks!.ShouldHaveSingleItem().Kind.ShouldBe(ValidationCheckKind.MainDatasetResult);
        replayBody!.AttemptId.ShouldBe(body.AttemptId);
        executor.RunCallCount.ShouldBe(1);

        var taskDetails = await HttpClient.GetFromJsonAsync<StudentTaskDetailsResponse>(
            ApiRoutes.Training.Student.ForTask(taskId), ClientJson.Options);
        taskDetails!.Validation.ShouldNotBeNull();
        taskDetails.Validation.Progress!.BestScore.ShouldBe(100);
        taskDetails.Validation.Hints.Groups.ShouldBe([HintGroup.Result]);

        var studentAttempt = await HttpClient.GetFromJsonAsync<StudentAttemptResponse>(
            ApiRoutes.Training.Student.ForAttempt(body.AttemptId), ClientJson.Options);
        studentAttempt!.Scoring.ShouldNotBeNull();
        studentAttempt.Scoring.Checks.ShouldHaveSingleItem();
        studentAttempt.Scoring.Hints.ShouldBeEmpty();

        var studentHistory = await HttpClient.GetFromJsonAsync<PageResponse<StudentAttemptListItemResponse>>(
            ApiRoutes.Training.Student.ForAttemptsPage(0, 100), ClientJson.Options);
        var studentItem = studentHistory!.Items.Single(value => value.Id == body.AttemptId);
        studentItem.ProgressId.ShouldBe(body.ProgressId);
        studentItem.ValidationVersionId.ShouldBe(body.ValidationVersionId);

        using var finalize = await PostAsync(
            ApiRoutes.Training.Student.ForTaskProgressFinalize(taskId), Guid.NewGuid());
        var finalization = await finalize.Content.ReadFromJsonAsync<ProgressFinalizationResponse>(ClientJson.Options);
        finalize.StatusCode.ShouldBe(HttpStatusCode.OK, await finalize.Content.ReadAsStringAsync());
        finalization!.Status.ShouldBe(ProgressStatus.Completed);
        finalization.FinalScore.ShouldBe(100);
        finalization.Reason.ShouldBe(FinalizationReason.PerfectScore);
        finalization.CanReturnToEducation.ShouldBeFalse();

        AsTeacher();
        var teacherAttempt = await HttpClient.GetFromJsonAsync<AttemptResponse>(
            ApiRoutes.Training.Attempts.ForId(body.AttemptId), ClientJson.Options);
        teacherAttempt!.Scoring.ShouldNotBeNull();
        teacherAttempt.Scoring.Checks.ShouldHaveSingleItem();

        var filtered = await HttpClient.GetFromJsonAsync<PageResponse<AttemptListItemResponse>>(
            $"{ApiRoutes.Training.Attempts.Collection}?progressId={body.ProgressId:D}" +
            $"&validationVersionId={body.ValidationVersionId:D}&scoreFrom=100&scoreTo=100" +
            "&finalizationReason=PerfectScore", ClientJson.Options);
        filtered!.Count.ShouldBe(1);
        filtered.Items.ShouldHaveSingleItem().Id.ShouldBe(body.AttemptId);
        var excluded = await HttpClient.GetFromJsonAsync<PageResponse<AttemptListItemResponse>>(
            $"{ApiRoutes.Training.Attempts.Collection}?scoreTo=99", ClientJson.Options);
        excluded!.Items.ShouldNotContain(value => value.Id == body.AttemptId);
        using var invalidRange = await HttpClient.GetAsync(
            $"{ApiRoutes.Training.Attempts.Collection}?scoreFrom=90&scoreTo=10");
        invalidRange.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.AttemptCheckResults.CountAsync(value => value.AttemptId == body.AttemptId)).ShouldBe(1);
        (await db.StudentTaskProgresses.AsNoTracking()
            .SingleAsync(value => value.Id == body.ProgressId)).BestScore.ShouldBe(100);
    }

    [Fact(DisplayName = "Finalization: standalone фиксирует BestScore и не создаёт platform outbox")]
    public async Task FinalizeStandalone_IsIdempotentAndDoesNotCreateOutbox()
    {
        var taskId = await CreatePublishedTaskAsync(null);
        AsStudent(Guid.NewGuid());
        await PostAsync(ApiRoutes.Training.Student.ForTaskProgress(taskId), Guid.NewGuid());
        var executor = App.Services.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<FakeSandboxExecutor>();
        executor.OverrideRun = Result<QueryResultSet>.Success(new QueryResultSet(
            true, null, ["unexpected"], [["value"]], 1, 1));

        using var submit = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(new SubmitAttemptRequest(taskId, "SELECT 2"), options: ClientJson.Options)
        };
        submit.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        using var submitted = await HttpClient.SendAsync(submit);
        submitted.StatusCode.ShouldBe(HttpStatusCode.Created, await submitted.Content.ReadAsStringAsync());

        var key = Guid.NewGuid();
        using var first = await PostAsync(ApiRoutes.Training.Student.ForTaskProgressFinalize(taskId), key);
        using var replay = await PostAsync(ApiRoutes.Training.Student.ForTaskProgressFinalize(taskId), key);
        var firstBody = await first.Content.ReadFromJsonAsync<ProgressFinalizationResponse>(ClientJson.Options);
        var replayBody = await replay.Content.ReadFromJsonAsync<ProgressFinalizationResponse>(ClientJson.Options);

        first.StatusCode.ShouldBe(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        replay.StatusCode.ShouldBe(HttpStatusCode.OK, await replay.Content.ReadAsStringAsync());
        firstBody!.Status.ShouldBe(ProgressStatus.Completed);
        firstBody.FinalScore.ShouldBe(0);
        firstBody.Reason.ShouldBe(FinalizationReason.Manual);
        replayBody.ShouldBe(firstBody);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.PendingPublishes.CountAsync(value => value.Kind == PendingPublishKind.Grade)).ShouldBe(0);
    }

    private async Task<HttpResponseMessage> PostAsync(string route, Guid key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("Idempotency-Key", key.ToString("D"));
        return await HttpClient.SendAsync(request);
    }

    private async Task<Guid> CreatePublishedTaskAsync(int? maxAttempts)
    {
        AsAdmin();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dbms = await DbmsDictionaryClient.CreateAsync(new CreateDbmsDictionaryRequest(
            $"Progress_{suffix}", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password"));
        AsTeacher();
        var targetDb = await TargetDbClient.CreateAsync(new CreateTargetDbRequest(
            dbms.Id, $"progress_{suffix}", null, false));
        var topic = await TopicClient.CreateAsync(new CreateTopicRequest($"Progress_{suffix}", null));
        var task = await SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
            topic.Id, $"Progress task {suffix}", "Проверить запрос", 2,
            new ReferenceQueryRequest(targetDb.Id, "SELECT 1", false, false)));
        var configuration = await HttpClient.GetFromJsonAsync<TaskValidationConfigurationResponse>(
            ApiRoutes.Training.SqlTasks.ForValidation(task.Id), ClientJson.Options);
        var update = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidation(task.Id),
            new TaskValidationConfigurationRequest(
                configuration!.Version, 100, maxAttempts, [HintGroup.Result],
                [new ValidationCheckRequest(
                    configuration.Checks[0].Id, ValidationCheckKind.MainDatasetResult, null, 100, 0)]),
            ClientJson.Options);
        var updated = (await update.Content.ReadFromJsonAsync<TaskValidationConfigurationResponse>(ClientJson.Options))!;
        using var publishRequest = new HttpRequestMessage(
            HttpMethod.Post, ApiRoutes.Training.SqlTasks.ForValidationPublish(task.Id))
        {
            Content = JsonContent.Create(
                new PublishTaskValidationRequest(updated.Version), options: ClientJson.Options)
        };
        publishRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        var publish = await HttpClient.SendAsync(publishRequest);
        publish.StatusCode.ShouldBe(HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());
        await SqlTaskClient.PublishAsync(task.Id);
        return task.Id;
    }
}
