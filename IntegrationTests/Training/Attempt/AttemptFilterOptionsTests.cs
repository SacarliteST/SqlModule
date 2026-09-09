using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.Attempt;

[Collection(IntegrationTestCollection.Name)]
public sealed class AttemptFilterOptionsTests : ApiTestBase
{
    public AttemptFilterOptionsTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private async Task<Fixture> SeedAsync()
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var studentName = "Иван-" + suffix;
        var studentEmail = $"ivan.{suffix}@example.com";
        var dbms = Domain.DbmsCatalog.DbmsDictionary.Create(
            "PostgreSql_" + suffix, "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var targetDb = TargetDb.Create(dbms.Id, "training_" + suffix, null, false);
        var joins = Domain.Training.Topic.Create("Соединения " + suffix);
        var basics = Domain.Training.Topic.Create("Основы " + suffix);
        var joinsQuery = Domain.Training.SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        var basicsQuery = Domain.Training.SqlQuery.Create("SELECT 2", false, false, targetDb.Id);
        var joinsTask = Domain.Training.SqlTask.Create(
            joins.Id, joinsQuery.Id, "Найти заказы " + suffix, "Text", 2,
            publicationStatus: PublicationStatus.Archived);
        var basicsTask = Domain.Training.SqlTask.Create(
            basics.Id, basicsQuery.Id, "Простой запрос " + suffix, "Text", 1,
            publicationStatus: PublicationStatus.Published);

        var ivanId = Guid.NewGuid();
        var annaId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var attempts = new[]
        {
            Domain.Training.Attempt.Record(ivanId, joinsTask.Id, "SELECT 1", ExecutionStatus.Succeeded,
                true, CheckReason.Ok, 1, 1, null, now.AddMinutes(-3), now.AddMinutes(-3),
                studentName: studentName, studentEmail: studentEmail),
            Domain.Training.Attempt.Record(ivanId, joinsTask.Id, "SELECT 1", ExecutionStatus.Succeeded,
                true, CheckReason.Ok, 1, 1, null, now.AddMinutes(-2), now.AddMinutes(-2),
                studentName: studentName, studentEmail: studentEmail),
            Domain.Training.Attempt.Record(annaId, basicsTask.Id, "SELECT 2", ExecutionStatus.Succeeded,
                true, CheckReason.Ok, 1, 1, null, now.AddMinutes(-1), now.AddMinutes(-1),
                studentName: "Анна Петрова", studentEmail: "anna@example.com")
        };

        db.AddRange(dbms, targetDb, joins, basics, joinsQuery, basicsQuery, joinsTask, basicsTask);
        db.Attempts.AddRange(attempts);
        await db.SaveChangesAsync();
        return new Fixture(suffix, ivanId, studentName, studentEmail, joins.Id, joinsTask.Id);
    }

    [Fact(DisplayName = "Attempt filters → поиск студентов по имени/email без дублей и восстановление по id")]
    public async Task Students_SearchPaginationAndRestore_Work()
    {
        var fixture = await SeedAsync();
        AsTeacher();

        var byName = await HttpClient.GetFromJsonAsync<PageResponse<AttemptStudentFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.StudentFilterOptions}?search={fixture.Suffix.ToUpperInvariant()}");
        byName!.Count.ShouldBe(1);
        byName.Items.Single().Email.ShouldBe(fixture.StudentEmail);

        var byEmail = await HttpClient.GetFromJsonAsync<PageResponse<AttemptStudentFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.StudentFilterOptions}?search={fixture.Suffix.ToUpperInvariant()}%40EXAMPLE&limit=1&offset=0");
        byEmail!.Count.ShouldBe(1);
        byEmail.Items.Single().Id.ShouldBe(fixture.StudentId);

        var restored = await HttpClient.GetFromJsonAsync<PageResponse<AttemptStudentFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.StudentFilterOptions}?id={fixture.StudentId}");
        restored!.Items.Single().DisplayName.ShouldBe(fixture.StudentName);

        var page = await HttpClient.GetFromJsonAsync<PageResponse<AttemptStudentFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.StudentFilterOptions}?limit=1&offset=0");
        page!.Count.ShouldNotBeNull();
        page.Count.Value.ShouldBeGreaterThanOrEqualTo(2);
        page.Items.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Attempt filters → темы и архивные задания ищутся и фильтруются по теме")]
    public async Task TopicsAndTasks_SearchAndTopicFilter_Work()
    {
        var fixture = await SeedAsync();
        AsAdmin();

        var topics = await HttpClient.GetFromJsonAsync<PageResponse<AttemptTopicFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.TopicFilterOptions}?search={fixture.Suffix.ToUpperInvariant()}");
        topics!.Count.ShouldBe(2);

        var tasks = await HttpClient.GetFromJsonAsync<PageResponse<AttemptTaskFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.TaskFilterOptions}?topicId={fixture.TopicId}&search=%D0%97%D0%90%D0%9A%D0%90%D0%97");
        tasks!.Count.ShouldBe(1);
        tasks.Items.Single().Id.ShouldBe(fixture.TaskId);

        var restored = await HttpClient.GetFromJsonAsync<PageResponse<AttemptTaskFilterOptionResponse>>(
            $"{ApiRoutes.Training.Attempts.TaskFilterOptions}?id={fixture.TaskId}");
        restored!.Items.Single().Id.ShouldBe(fixture.TaskId);
    }

    [Fact(DisplayName = "Attempt filters → Student получает 403, anonymous 401")]
    public async Task Authorization_IsContentAuthorOnly()
    {
        AsStudent();
        (await HttpClient.GetAsync(ApiRoutes.Training.Attempts.StudentFilterOptions))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        HttpClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        (await HttpClient.GetAsync(ApiRoutes.Training.Attempts.TaskFilterOptions))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Anonymous");
    }

    [Theory(DisplayName = "Attempt filters → невалидная пагинация и длинный поиск возвращают 422")]
    [InlineData("?offset=-1")]
    [InlineData("?limit=0")]
    [InlineData("?limit=101")]
    [InlineData("?id=not-a-guid")]
    public async Task InvalidPagination_Returns422(string query)
    {
        AsTeacher();
        (await HttpClient.GetAsync(ApiRoutes.Training.Attempts.StudentFilterOptions + query))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "Attempt filters → некорректный topicId возвращает 422")]
    public async Task InvalidTopicId_Returns422()
    {
        AsTeacher();
        (await HttpClient.GetAsync(ApiRoutes.Training.Attempts.TaskFilterOptions + "?topicId=wrong"))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "Attempt filters → поиск длиннее 200 символов возвращает 422")]
    public async Task TooLongSearch_Returns422()
    {
        AsTeacher();
        var search = new string('x', 201);
        (await HttpClient.GetAsync(ApiRoutes.Training.Attempts.TopicFilterOptions + "?search=" + search))
            .StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record Fixture(
        string Suffix, Guid StudentId, string StudentName, string StudentEmail, Guid TopicId, Guid TaskId);
}
