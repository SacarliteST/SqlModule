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
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.Training.Progress;

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
        var publish = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.SqlTasks.ForValidationPublish(task.Id),
            new PublishTaskValidationRequest(updated.Version), ClientJson.Options);
        publish.StatusCode.ShouldBe(HttpStatusCode.OK, await publish.Content.ReadAsStringAsync());
        await SqlTaskClient.PublishAsync(task.Id);
        return task.Id;
    }
}
