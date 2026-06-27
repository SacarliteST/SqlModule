using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.Attempt;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.SqlQuery;
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
    }

    private void SetFakeRun(QueryResultSet resultSet)
    {
        var executor = (FakeSandboxExecutor)App.Services.GetRequiredService<ISandboxExecutor>();
        executor.OverrideRun = Result<QueryResultSet>.Success(resultSet);
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

    private async Task<Guid> CreateTaskAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));
        var sqlQuery = await SqlQueryClient.CreateAsync(
            new CreateSqlQueryRequest(targetDb.Id, "SELECT 1", false, false));

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = DomainSqlTask.Create(
            topic.Id, sqlQuery.Id, "Task_" + Guid.NewGuid(), "Text", 1);
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
        var attempt = await AttemptClient.GetByIdAsync(response.AttemptId);
        attempt.ShouldNotBeNull();
        attempt!.UserId.ShouldBe(studentId);
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
        response.ErrorMessage.ShouldBe("syntax error");
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

        // Act
        var page = await AttemptClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(a => a.Id == submitted.AttemptId);
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
}
