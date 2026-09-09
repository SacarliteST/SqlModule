using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.Attempt;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Isolated;
using DomainSqlTask = SQLModule.Domain.Training.SqlTask;

namespace SQLModule.IntegrationTests.Training.Attempt;

/// <summary>
/// Интеграционные тесты для слайса SubmitAttempt и просмотра истории попыток.
/// Каждый тест независим: создаёт собственные данные.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AttemptTests : ApiTestBase
{
    public AttemptTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private FakeSandboxExecutor GetFakeExecutor()
    {
        using var scope = App.Services.CreateScope();
        return (FakeSandboxExecutor)scope.ServiceProvider.GetRequiredService<ISandboxExecutor>();
    }

    private void ResetFakeExecutor()
    {
        // FakeSandboxExecutor is singleton so we can cast directly from the root container
        var executor = (FakeSandboxExecutor)App.Services.GetRequiredService<ISandboxExecutor>();
        executor.OverrideRun = null;
        executor.OverrideSetup = null;
        executor.ResetRunCallCount();
    }

    private void SetFakeRun(QueryResultSet resultSet)
    {
        var executor = (FakeSandboxExecutor)App.Services.GetRequiredService<ISandboxExecutor>();
        executor.OverrideRun = Result<QueryResultSet>.Success(resultSet);
    }

    private async Task<HttpResponseMessage> SubmitRawAsync(
        SubmitAttemptRequest request, string? idempotencyKey)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(request)
        };
        if (idempotencyKey is not null)
        {
            message.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await HttpClient.SendAsync(message);
    }

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = Domain.DbmsCatalog.DbmsDictionary.Create(
            "Test_" + Guid.NewGuid(), "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);
        await db.SaveChangesAsync();
        return dbms.Id;
    }

    private async Task<Guid> CreateTaskAsync(
        PublicationStatus status = PublicationStatus.Published,
        bool strictRowOrder = false)
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));
        var sqlQuery = await SqlQueryClient.CreateAsync(
            new CreateSqlQueryRequest(targetDb.Id, "SELECT 1", false, strictRowOrder));

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = DomainSqlTask.Create(
            topic.Id, sqlQuery.Id, "Task_" + Guid.NewGuid(), "Text", 1,
            publicationStatus: status);
        db.SqlTasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    [Fact(DisplayName = "Submit → возвращает SubmitAttemptResponse с IsCorrect=true при совпадении")]
    public async Task Submit_MatchingResult_ReturnsCorrect()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();

        // Act
        var response = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT 1"));

        // Assert
        response.AttemptId.ShouldNotBe(Guid.Empty);
        response.Status.ShouldBe(ExecutionStatus.Succeeded);
        response.IsCorrect.ShouldBeTrue();
        response.Reason.ShouldBe(CheckReason.Ok);
    }

    [Fact(DisplayName = "Submit → UserId в сохранённой попытке совпадает с userId текущего студента")]
    public async Task Submit_WithAuth_UserIdEqualsCurrentStudentId()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        var studentId = Guid.NewGuid();
        AsStudent(userId: studentId);

        // Act
        var response = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT 1"));

        // Assert: verify via GetById
        AsTeacher();
        var attempt = await AttemptClient.GetByIdAsync(response.AttemptId);
        attempt.ShouldNotBeNull();
        attempt!.UserId.ShouldBe(studentId);
        attempt.StudentName.ShouldBe("Test Student");
    }

    [Fact(DisplayName = "Submit → SQL-ошибка создаёт Attempt со Status=Error")]
    public async Task Submit_SqlError_CreatesAttemptWithStatusError()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        SetFakeRun(new QueryResultSet(false, "syntax error", [], [], 0, 0));
        AsStudent();

        // Act
        var response = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "INVALID SQL"));

        // Assert
        response.Status.ShouldBe(ExecutionStatus.Error);
        response.IsCorrect.ShouldBeFalse();
        response.Reason.ShouldBe(CheckReason.SqlError);
        response.PublicError.ShouldNotBeNullOrWhiteSpace();
        response.PublicError.ShouldNotContain("syntax error");
    }

    [Fact(DisplayName = "Submit → несовпадающий результат IsCorrect=false с причиной")]
    public async Task Submit_ColumnMismatch_ReturnsColumnMismatchReason()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        // Golden is empty columns/rows; return a result with columns to cause ColumnMismatch
        SetFakeRun(new QueryResultSet(true, null, ["id"], [["1"]], 1, 5));
        AsStudent();

        // Act
        var response = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT id FROM t"));

        // Assert
        response.Status.ShouldBe(ExecutionStatus.Succeeded);
        response.IsCorrect.ShouldBeFalse();
        response.Reason.ShouldBe(CheckReason.ColumnMismatch);
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную попытку")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        var submitted = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT 1"));
        AsTeacher();

        // Act
        var found = await AttemptClient.GetByIdAsync(submitted.AttemptId);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(submitted.AttemptId);
        found.TaskId.ShouldBe(taskId);
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную попытку")]
    public async Task GetAll_ContainsSubmittedAttempt()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        var submitted = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT 1"));
        AsTeacher();

        // Act
        var page = await AttemptClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(a => a.Id == submitted.AttemptId);
        var attempt = page.Items.Single(a => a.Id == submitted.AttemptId);
        attempt.CreatedAt.ShouldBe(attempt.StartedAt);
        attempt.UpdatedAt.ShouldBe(attempt.FinishedAt);
        attempt.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        attempt.CreatedById.ShouldNotBe(Guid.Empty);
        attempt.CreatedByName.ShouldBe("Test Student");
    }

    [Fact(DisplayName = "Delete → попытка больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        var submitted = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT 1"));

        // Act
        AsAdmin();
        await AttemptClient.DeleteAsync(submitted.AttemptId);

        // Assert
        var found = await AttemptClient.GetByIdAsync(submitted.AttemptId);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var result = await AttemptClient.GetByIdAsync(Guid.NewGuid());
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        AsAdmin();
        await Should.NotThrowAsync(() => AttemptClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Submit с несуществующим TaskId → NotFoundException")]
    public async Task Submit_UnknownTaskId_ThrowsNotFoundException()
    {
        AsStudent();
        await Should.ThrowAsync<NotFoundException>(
            () => AttemptClient.SubmitAsync(
                new SubmitAttemptRequest(Guid.NewGuid(), "SELECT 1")));
    }

    [Fact(DisplayName = "Submit с пустым SubmittedSql → ValidationException")]
    public async Task Submit_EmptySubmittedSql_ThrowsValidationException()
    {
        AsStudent();
        var ex = await Should.ThrowAsync<ValidationException>(
            () => AttemptClient.SubmitAsync(
                new SubmitAttemptRequest(Guid.NewGuid(), "")));

        ex.Errors.ShouldContainKey("SubmittedSql");
    }

    [Fact(DisplayName = "Student API → каталог содержит только Published, закрытые карточки возвращают 404")]
    public async Task StudentCatalog_OnlyPublishedAndClosedTasksAreHidden()
    {
        var publishedId = await CreateTaskAsync(PublicationStatus.Published);
        var draftId = await CreateTaskAsync(PublicationStatus.Draft);
        var archivedId = await CreateTaskAsync(PublicationStatus.Archived);
        AsStudent();

        using var listResponse = await HttpClient.GetAsync(
            ApiRoutes.Training.Student.ForTasksPage(offset: 0, limit: 100));
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await listResponse.Content.ReadAsStringAsync();
        json.ShouldContain(publishedId.ToString());
        json.ShouldNotContain(draftId.ToString());
        json.ShouldNotContain(archivedId.ToString());
        json.ShouldNotContain("sqlQueryId", Case.Insensitive);
        json.ShouldNotContain("expectedResult", Case.Insensitive);
        json.ShouldNotContain("queryText", Case.Insensitive);

        (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTask(draftId)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForTask(archivedId)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Student topics → скрывает темы и ветки без Published заданий")]
    public async Task StudentTopics_HideBranchesWithoutPublishedTasks()
    {
        var publishedTaskId = await CreateTaskAsync(PublicationStatus.Published);
        var draftTaskId = await CreateTaskAsync(PublicationStatus.Draft);
        var publishedTask = await SqlTaskClient.GetByIdAsync(publishedTaskId);
        var draftTask = await SqlTaskClient.GetByIdAsync(draftTaskId);
        AsStudent();

        using var response = await HttpClient.GetAsync(ApiRoutes.Training.Student.Topics);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain(publishedTask!.TopicId.ToString());
        json.ShouldNotContain(draftTask!.TopicId.ToString());
    }

    [Theory(DisplayName = "Submit → Draft/Archived отклоняются до запуска sandbox")]
    [InlineData(PublicationStatus.Draft)]
    [InlineData(PublicationStatus.Archived)]
    public async Task Submit_ClosedTask_DoesNotRunSandbox(PublicationStatus status)
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync(status);
        ResetFakeExecutor();
        AsStudent();
        var executor = GetFakeExecutor();

        using var response = await SubmitRawAsync(
            new SubmitAttemptRequest(taskId, "SELECT 1"), Guid.NewGuid().ToString());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        executor.RunCallCount.ShouldBe(0);
    }

    [Fact(DisplayName = "Student history → items и count принадлежат только текущему студенту")]
    public async Task StudentHistory_IsAlwaysOwnerScoped()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        var ownerId = Guid.NewGuid();
        AsStudent(ownerId);
        var own = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"));
        AsStudent(Guid.NewGuid());
        var foreign = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"));
        AsStudent(ownerId);

        using var response = await HttpClient.GetAsync(
            ApiRoutes.Training.Student.ForAttemptsPage(offset: 0, limit: 100));
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("count").GetInt32().ShouldBe(1);
        var items = json.RootElement.GetProperty("items");
        items.GetArrayLength().ShouldBe(1);
        items[0].GetProperty("id").GetGuid().ShouldBe(own.AttemptId);
        items[0].TryGetProperty("userId", out _).ShouldBeFalse();
        items[0].TryGetProperty("studentName", out _).ShouldBeFalse();
        items[0].TryGetProperty("errorMessage", out _).ShouldBeFalse();

        (await HttpClient.GetAsync(ApiRoutes.Training.Student.ForAttempt(foreign.AttemptId)))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Student endpoints → Anonymous получает 401, Teacher и Admin получают 403")]
    public async Task StudentEndpoints_EnforceStudentPolicy()
    {
        var id = Guid.NewGuid();
        string[] endpoints =
        [
            ApiRoutes.Training.Student.Topics,
            ApiRoutes.Training.Student.Tasks,
            ApiRoutes.Training.Student.ForTask(id),
            ApiRoutes.Training.Student.ForTaskSchema(id),
            ApiRoutes.Training.Student.Attempts,
            ApiRoutes.Training.Student.ForAttempt(id)
        ];

        HttpClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        foreach (var endpoint in endpoints)
        {
            (await HttpClient.GetAsync(endpoint)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Anonymous");

        AsTeacher();
        foreach (var endpoint in endpoints)
        {
            (await HttpClient.GetAsync(endpoint)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
        AsAdmin();
        foreach (var endpoint in endpoints)
        {
            (await HttpClient.GetAsync(endpoint)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }

    [Fact(DisplayName = "Delete attempt → Student и Teacher получают 403, Admin удаляет")]
    public async Task DeleteAttempt_IsAdminOnly()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        var attempt = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"));

        (await HttpClient.DeleteAsync(ApiRoutes.Training.Attempts.ForId(attempt.AttemptId)))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        AsTeacher();
        (await HttpClient.DeleteAsync(ApiRoutes.Training.Attempts.ForId(attempt.AttemptId)))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        AsAdmin();
        (await HttpClient.DeleteAsync(ApiRoutes.Training.Attempts.ForId(attempt.AttemptId)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact(DisplayName = "Submit → SQL error безопасен, превышение MaxRows помечает результат усечённым")]
    public async Task Submit_PublicErrorAndTruncationAreSafe()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        SetFakeRun(new QueryResultSet(false, "password=secret; container=abc", [], [], 0, 1));
        var failed = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "INVALID SQL"));
        failed.PublicError.ShouldNotBeNullOrWhiteSpace();
        failed.PublicError.ShouldNotContain("secret");
        failed.PublicError.ShouldNotContain("container");

        SetFakeRun(new QueryResultSet(true, null, [], [], 1000, 1, IsTruncated: true));
        var truncated = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"));
        truncated.IsResultTruncated.ShouldBeTrue();
    }

    [Fact(DisplayName = "Result snapshot → POST, student details и teacher details возвращают одинаковые NULL-ячейки")]
    public async Task ResultSnapshot_IsPersistedForStudentAndTeacherDetails()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        var studentId = Guid.NewGuid();
        AsStudent(studentId);
        SetFakeRun(new QueryResultSet(
            true, null, ["id", "name"],
            [["1", "Иван"], ["2", null]], 2, 42));

        var submitted = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT id, name"));
        var studentDetails = await HttpClient.GetFromJsonAsync<StudentAttemptResponse>(
            ApiRoutes.Training.Student.ForAttempt(submitted.AttemptId), ClientJson.Options);

        studentDetails.ShouldNotBeNull();
        studentDetails.ResultSnapshotState.ShouldBe(AttemptResultSnapshotState.Available);
        JsonSerializer.Serialize(studentDetails.ActualColumns).ShouldBe(JsonSerializer.Serialize(submitted.ActualColumns));
        JsonSerializer.Serialize(studentDetails.ActualRows).ShouldBe(JsonSerializer.Serialize(submitted.ActualRows));
        studentDetails.ActualRows![1][1].ShouldBeNull();
        studentDetails.ReturnedRowCount.ShouldBe(2);

        AsTeacher();
        var teacherDetails = await AttemptClient.GetByIdAsync(submitted.AttemptId);
        teacherDetails.ShouldNotBeNull();
        JsonSerializer.Serialize(teacherDetails.ActualRows).ShouldBe(JsonSerializer.Serialize(submitted.ActualRows));

        using var listResponse = await HttpClient.GetAsync(ApiRoutes.Training.Attempts.ForPagination(0, 20));
        var listJson = await listResponse.Content.ReadAsStringAsync();
        listJson.ShouldNotContain("actualRows", Case.Insensitive);
        listJson.ShouldNotContain("actualColumns", Case.Insensitive);
    }

    [Fact(DisplayName = "Result snapshot → runtime публикует обязательное строковое состояние во всех detail-ответах")]
    public async Task ResultSnapshotState_IsRequiredNamedStringInRuntimeJson()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        SetFakeRun(new QueryResultSet(true, null, ["id"], [["1"]], 1, 1));

        using var submitResponse = await SubmitRawAsync(
            new SubmitAttemptRequest(taskId, "SELECT id"), Guid.NewGuid().ToString());
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var submitJson = await submitResponse.Content.ReadAsStringAsync();
        submitJson.ShouldContain("\"resultSnapshotState\":\"Available\"");
        var submitted = JsonSerializer.Deserialize<SubmitAttemptResponse>(submitJson, ClientJson.Options);
        submitted.ShouldNotBeNull();

        using var studentResponse = await HttpClient.GetAsync(
            ApiRoutes.Training.Student.ForAttempt(submitted.AttemptId));
        studentResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var studentJson = await studentResponse.Content.ReadAsStringAsync();
        studentJson.ShouldContain("\"resultSnapshotState\":\"Available\"");

        AsTeacher();
        using var teacherResponse = await HttpClient.GetAsync(
            ApiRoutes.Training.Attempts.ForId(submitted.AttemptId));
        teacherResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var teacherJson = await teacherResponse.Content.ReadAsStringAsync();
        teacherJson.ShouldContain("\"resultSnapshotState\":\"Available\"");
    }

    [Fact(DisplayName = "Result snapshot → пустой успешный результат отличается от отсутствующего")]
    public async Task EmptyResult_IsAvailableWithEmptyRows()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        SetFakeRun(new QueryResultSet(true, null, ["id"], [], 0, 1));

        var submitted = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT id"));
        var details = await HttpClient.GetFromJsonAsync<StudentAttemptResponse>(
            ApiRoutes.Training.Student.ForAttempt(submitted.AttemptId), ClientJson.Options);

        details!.ResultSnapshotState.ShouldBe(AttemptResultSnapshotState.Available);
        details.ActualColumns.ShouldBe(["id"]);
        details.ActualRows.ShouldBeEmpty();
        details.ReturnedRowCount.ShouldBe(0);
        details.IsResultTruncated.ShouldBeFalse();
    }

    [Fact(DisplayName = "Result snapshot → MaxRows+1 усекается, ровно MaxRows не помечается")]
    public async Task ResultSnapshot_TruncationRequiresAdditionalRow()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        var exactlyLimit = Enumerable.Range(0, 200)
            .Select(i => (IReadOnlyList<string?>)[i.ToString()]).ToList();
        SetFakeRun(new QueryResultSet(true, null, ["id"], exactlyLimit, 200, 1));
        var exact = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT exact"));
        exact.IsResultTruncated.ShouldBeFalse();
        exact.ReturnedRowCount.ShouldBe(200);
        exact.ResultRowLimit.ShouldBe(200);

        var overLimit = Enumerable.Range(0, 201)
            .Select(i => (IReadOnlyList<string?>)[i.ToString()]).ToList();
        SetFakeRun(new QueryResultSet(true, null, ["id"], overLimit, 201, 1));
        var truncated = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT over"));
        truncated.IsResultTruncated.ShouldBeTrue();
        truncated.ReturnedRowCount.ShouldBe(200);
        truncated.ActualRows!.Count.ShouldBe(200);
    }

    [Fact(DisplayName = "Comparison limit → 250 строк сравниваются полностью, snapshot сохраняет 200")]
    public async Task ComparisonLimit_IsIndependentFromSnapshotLimit()
    {
        ResetFakeExecutor();
        var rows = Enumerable.Range(1, 250)
            .Select(value => (IReadOnlyList<string?>)[value.ToString()])
            .ToList();
        SetFakeRun(new QueryResultSet(true, null, ["id"], rows, rows.Count, 1));
        var taskId = await CreateTaskAsync(strictRowOrder: true);

        AsStudent();
        SetFakeRun(new QueryResultSet(true, null, ["id"], rows, rows.Count, 1));
        var matching = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT generate_series(1, 250)"));

        matching.IsCorrect.ShouldBeTrue();
        matching.Reason.ShouldBe(CheckReason.Ok);
        matching.RowCount.ShouldBe(250);
        matching.ReturnedRowCount.ShouldBe(200);
        matching.ActualRows!.Count.ShouldBe(200);
        matching.IsResultTruncated.ShouldBeTrue();
        matching.ResultRowLimit.ShouldBe(200);
        GetFakeExecutor().LastQuery!.MaxRows.ShouldBe(10000);

        var changedAfterSnapshot = rows.Select(row => (IReadOnlyList<string?>)row.ToArray()).ToList();
        changedAfterSnapshot[200] = ["different"];
        SetFakeRun(new QueryResultSet(
            true, null, ["id"], changedAfterSnapshot, changedAfterSnapshot.Count, 1));
        var mismatch = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT changed_after_200"));
        mismatch.IsCorrect.ShouldBeFalse();
        mismatch.Reason.ShouldBe(CheckReason.ValueMismatch);
    }

    [Fact(DisplayName = "Comparison limit → порядок после snapshot учитывает StrictRowOrder")]
    public async Task ComparisonAfterSnapshot_RespectsStrictRowOrder()
    {
        ResetFakeExecutor();
        var rows = Enumerable.Range(1, 250)
            .Select(value => (IReadOnlyList<string?>)[value.ToString()])
            .ToList();
        SetFakeRun(new QueryResultSet(true, null, ["id"], rows, rows.Count, 1));
        var strictTaskId = await CreateTaskAsync(strictRowOrder: true);
        SetFakeRun(new QueryResultSet(true, null, ["id"], rows, rows.Count, 1));
        var unorderedTaskId = await CreateTaskAsync(strictRowOrder: false);

        var reordered = rows.Select(row => (IReadOnlyList<string?>)row.ToArray()).ToList();
        (reordered[200], reordered[201]) = (reordered[201], reordered[200]);
        AsStudent();
        SetFakeRun(new QueryResultSet(true, null, ["id"], reordered, reordered.Count, 1));
        var strict = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(strictTaskId, "SELECT reordered_strict"));
        strict.Reason.ShouldBe(CheckReason.ValueMismatch);

        SetFakeRun(new QueryResultSet(true, null, ["id"], reordered, reordered.Count, 1));
        var unordered = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(unorderedTaskId, "SELECT reordered_unordered"));
        unordered.IsCorrect.ShouldBeTrue();
        unordered.Reason.ShouldBe(CheckReason.Ok);
    }

    [Fact(DisplayName = "Comparison limit → превышение студентом возвращает ResultLimitExceeded")]
    public async Task StudentResultOverComparisonLimit_ReturnsDedicatedReason()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        var rows = Enumerable.Range(1, 10000)
            .Select(value => (IReadOnlyList<string?>)[value.ToString()])
            .ToList();
        SetFakeRun(new QueryResultSet(true, null, ["id"], rows, rows.Count, 1, IsTruncated: true));

        var response = await AttemptClient.SubmitAsync(
            new SubmitAttemptRequest(taskId, "SELECT too_many_rows"));

        response.IsCorrect.ShouldBeFalse();
        response.Reason.ShouldBe(CheckReason.ResultLimitExceeded);
        response.PublicError.ShouldNotBeNull().ShouldContain("10000");
        response.ReturnedRowCount.ShouldBe(200);
        response.IsResultTruncated.ShouldBeTrue();
        GetFakeExecutor().OverrideRun = null;
    }

    [Theory(DisplayName = "Result snapshot → SQL error и timeout имеют NotProduced без raw diagnostics")]
    [InlineData("syntax error near password=secret")]
    [InlineData("query timeout; container=private")]
    public async Task FailedExecution_HasSafeNotProducedSnapshot(string rawError)
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        AsStudent();
        SetFakeRun(new QueryResultSet(false, rawError, [], [], 0, 1));

        var submitted = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "INVALID"));
        submitted.ResultSnapshotState.ShouldBe(AttemptResultSnapshotState.NotProduced);
        submitted.ActualColumns.ShouldBeNull();
        submitted.ActualRows.ShouldBeNull();
        submitted.PublicError.ShouldNotBeNull();
        submitted.PublicError.ShouldNotContain("secret");
        submitted.PublicError.ShouldNotContain("container");

        var details = await HttpClient.GetFromJsonAsync<StudentAttemptResponse>(
            ApiRoutes.Training.Student.ForAttempt(submitted.AttemptId), ClientJson.Options);
        details!.ResultSnapshotState.ShouldBe(AttemptResultSnapshotState.NotProduced);
        details.ActualRows.ShouldBeNull();
        JsonSerializer.Serialize(details).ShouldNotContain(rawError);
    }

    [Fact(DisplayName = "Result snapshot → старая попытка NotStored, истёкшая Expired, sandbox не запускается")]
    public async Task OldAndExpiredSnapshots_DoNotRerunSandbox()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        // Создание эталонного запроса выполняет его для валидации и не относится
        // к чтению истории попыток, которое проверяется ниже.
        ResetFakeExecutor();
        var studentId = Guid.NewGuid();
        AsStudent(studentId);
        var oldAttempt = Domain.Training.Attempt.Record(
            studentId, taskId, "SELECT old", ExecutionStatus.Succeeded, false,
            CheckReason.ValueMismatch, 1, 1, null,
            DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(-60));
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Attempts.Add(oldAttempt);
            await db.SaveChangesAsync();
        }

        var oldDetails = await HttpClient.GetFromJsonAsync<StudentAttemptResponse>(
            ApiRoutes.Training.Student.ForAttempt(oldAttempt.Id), ClientJson.Options);
        oldDetails!.ResultSnapshotState.ShouldBe(AttemptResultSnapshotState.NotStored);
        GetFakeExecutor().RunCallCount.ShouldBe(0);

        SetFakeRun(new QueryResultSet(true, null, ["id"], [["1"]], 1, 1));
        var submitted = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT new"));
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Attempts.Where(x => x.Id == submitted.AttemptId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    x => x.ResultSnapshotExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }
        var callsBeforeRead = GetFakeExecutor().RunCallCount;

        var expired = await HttpClient.GetFromJsonAsync<StudentAttemptResponse>(
            ApiRoutes.Training.Student.ForAttempt(submitted.AttemptId), ClientJson.Options);
        expired!.ResultSnapshotState.ShouldBe(AttemptResultSnapshotState.Expired);
        expired.ActualColumns.ShouldBeNull();
        expired.ActualRows.ShouldBeNull();
        expired.ResultSnapshotCreatedAt.ShouldNotBeNull();
        expired.ResultSnapshotExpiresAt.ShouldNotBeNull();
        GetFakeExecutor().RunCallCount.ShouldBe(callsBeforeRead);
    }

    [Fact(DisplayName = "Submit idempotency → последовательный повтор возвращает ту же попытку без sandbox")]
    public async Task Submit_SameKeyAndPayload_ReplaysCompletedResponse()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        ResetFakeExecutor();
        AsStudent();
        var key = Guid.NewGuid();

        var first = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"), key);
        var replay = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"), key);

        JsonSerializer.Serialize(replay).ShouldBe(JsonSerializer.Serialize(first));
        GetFakeExecutor().RunCallCount.ShouldBe(1);
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(attempt => attempt.Id == first.AttemptId)).ShouldBe(1);
    }

    [Fact(DisplayName = "Submit idempotency → конкурентные повторы создают одну попытку")]
    public async Task Submit_ConcurrentSameKeyAndPayload_CreatesOneAttempt()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        ResetFakeExecutor();
        var studentId = Guid.NewGuid();
        AsStudent(studentId);
        var key = Guid.NewGuid();
        var request = new SubmitAttemptRequest(taskId, "SELECT 1");

        var responses = await Task.WhenAll(
            AttemptClient.SubmitAsync(request, key),
            AttemptClient.SubmitAsync(request, key));

        JsonSerializer.Serialize(responses[0]).ShouldBe(JsonSerializer.Serialize(responses[1]));
        GetFakeExecutor().RunCallCount.ShouldBe(1);
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Attempts.CountAsync(attempt => attempt.UserId == studentId && attempt.TaskId == taskId))
            .ShouldBe(1);
    }

    [Fact(DisplayName = "Submit idempotency → тот же ключ с другим SQL или taskId возвращает 409")]
    public async Task Submit_SameKeyWithDifferentPayload_ReturnsConflict()
    {
        ResetFakeExecutor();
        var firstTaskId = await CreateTaskAsync();
        var secondTaskId = await CreateTaskAsync();
        ResetFakeExecutor();
        AsStudent();
        var key = Guid.NewGuid().ToString();

        using var first = await SubmitRawAsync(new SubmitAttemptRequest(firstTaskId, "SELECT 1"), key);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var differentSql = await SubmitRawAsync(new SubmitAttemptRequest(firstTaskId, "SELECT 2"), key);
        using var differentTask = await SubmitRawAsync(new SubmitAttemptRequest(secondTaskId, "SELECT 1"), key);

        differentSql.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        differentTask.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        foreach (var response in new[] { differentSql, differentTask })
        {
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("code").GetString()
                .ShouldBe("IdempotencyKeyPayloadMismatch");
        }
        GetFakeExecutor().RunCallCount.ShouldBe(1);
    }

    [Fact(DisplayName = "Submit idempotency → область ключа изолирована по студенту")]
    public async Task Submit_SameKeyForDifferentStudents_CreatesSeparateAttempts()
    {
        ResetFakeExecutor();
        var taskId = await CreateTaskAsync();
        ResetFakeExecutor();
        var key = Guid.NewGuid();

        AsStudent(Guid.NewGuid());
        var first = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"), key);
        AsStudent(Guid.NewGuid());
        var second = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"), key);

        second.AttemptId.ShouldNotBe(first.AttemptId);
        GetFakeExecutor().RunCallCount.ShouldBe(2);
    }

    [Theory(DisplayName = "Submit idempotency → отсутствующий или некорректный ключ возвращает безопасный 400")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-uuid")]
    public async Task Submit_InvalidIdempotencyKey_ReturnsBadRequest(string? key)
    {
        AsStudent();

        using var response = await SubmitRawAsync(
            new SubmitAttemptRequest(Guid.NewGuid(), "SELECT 1"), key);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().ShouldBe("InvalidIdempotencyKey");
    }

    [Fact(DisplayName = "Submit idempotency → истёкший ключ создаёт новую попытку и заменяет receipt")]
    public async Task Submit_ExpiredKey_CreatesNewAttempt()
    {
        var taskId = await CreateTaskAsync();
        ResetFakeExecutor();
        var studentId = Guid.NewGuid();
        AsStudent(studentId);
        var key = Guid.NewGuid();
        var first = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"), key);

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var receiptScope = $"attempt:{studentId}:submit";
            await db.MutationReceipts
                .Where(receipt => receipt.Scope == receiptScope && receipt.IdempotencyKey == key.ToString())
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    receipt => receipt.CreatedAt, DateTimeOffset.UtcNow.AddHours(-25)));
        }

        var second = await AttemptClient.SubmitAsync(new SubmitAttemptRequest(taskId, "SELECT 1"), key);

        second.AttemptId.ShouldNotBe(first.AttemptId);
        GetFakeExecutor().RunCallCount.ShouldBe(2);
        using var verificationScope = App.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verificationDb.MutationReceipts.CountAsync(receipt =>
            receipt.Scope == $"attempt:{studentId}:submit" && receipt.IdempotencyKey == key.ToString()))
            .ShouldBe(1);
    }
}
