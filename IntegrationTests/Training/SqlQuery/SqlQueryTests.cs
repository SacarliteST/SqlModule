using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.SqlQuery;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using DomainAttempt = SQLModule.Domain.Training.Attempt;
using DomainSqlTask = SQLModule.Domain.Training.SqlTask;

namespace SQLModule.IntegrationTests.Training.SqlQuery;

/// <summary>
/// Интеграционные тесты CRUD-операций для <see cref="ISqlQueryClient"/>.
/// Каждый тест независим: создаёт собственные данные через вспомогательные методы.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SqlQueryTests : ApiTestBase
{
    private readonly TestApplication app;

    public SqlQueryTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    /// <summary>Создаёт SQL-запрос через API и возвращает ответ сервера.</summary>
    private async Task<SqlQueryResponse> CreateSqlQueryAsync(string queryText = "SELECT 1")
        => await SqlQueryClient.CreateAsync(new CreateSqlQueryRequest(queryText, false, false));

    /// <summary>Создаёт DbmsDictionary напрямую через AppDbContext и возвращает Id.</summary>
    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = Domain.DbmsCatalog.DbmsDictionary.Create(
            "Test_" + Guid.NewGuid(), "test", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);
        await db.SaveChangesAsync();
        return dbms.Id;
    }

    /// <summary>
    /// Создаёт SqlTask напрямую через AppDbContext и возвращает Id.
    /// Параметры должны ссылаться на уже существующие в БД записи.
    /// </summary>
    private async Task<Guid> SeedSqlTaskAsync(Guid targetDbId, Guid topicId, Guid sqlQueryId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = DomainSqlTask.Create(targetDbId, topicId, sqlQueryId, "Task_" + Guid.NewGuid(), "Text", 1);
        db.SqlTasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    /// <summary>
    /// Создаёт Attempt напрямую через AppDbContext.
    /// taskId и queryId должны ссылаться на уже существующие в БД записи.
    /// </summary>
    private async Task SeedAttemptAsync(Guid taskId, Guid queryId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = DomainAttempt.Create(
            Guid.NewGuid(), true,
            DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow,
            taskId, queryId);
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync();
    }

    // happy-path

    [Fact(DisplayName = "Create → возвращает SqlQueryResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new CreateSqlQueryRequest("SELECT id FROM users", true, false);

        // Act
        var response = await SqlQueryClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.QueryText.ShouldBe("SELECT id FROM users");
        response.StrictColumnOrder.ShouldBeTrue();
        response.StrictRowOrder.ShouldBeFalse();
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданный запрос")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var created = await CreateSqlQueryAsync("SELECT 42");

        // Act
        var found = await SqlQueryClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.QueryText.ShouldBe("SELECT 42");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданный запрос")]
    public async Task GetAll_ContainsCreatedQuery()
    {
        // Arrange
        var created = await CreateSqlQueryAsync();

        // Act
        var page = await SqlQueryClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(q => q.Id == created.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var created = await CreateSqlQueryAsync("SELECT 1");

        // Act
        await SqlQueryClient.UpdateAsync(created.Id, new UpdateSqlQueryRequest("SELECT 2", true, true));

        // Assert
        var updated = await SqlQueryClient.GetByIdAsync(created.Id);
        updated!.QueryText.ShouldBe("SELECT 2");
        updated.StrictColumnOrder.ShouldBeTrue();
        updated.StrictRowOrder.ShouldBeTrue();
    }

    [Fact(DisplayName = "Delete → запрос больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var created = await CreateSqlQueryAsync();

        // Act
        await SqlQueryClient.DeleteAsync(created.Id);

        // Assert
        var found = await SqlQueryClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    // негатив

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var result = await SqlQueryClient.GetByIdAsync(unknownId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act + Assert
        await Should.NotThrowAsync(() => SqlQueryClient.DeleteAsync(unknownId));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        var request = new UpdateSqlQueryRequest("SELECT 1", false, false);

        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => SqlQueryClient.UpdateAsync(unknownId, request));
    }

    [Fact(DisplayName = "Create с пустым QueryText → ValidationException")]
    public async Task Create_EmptyQueryText_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateSqlQueryRequest("", false, false);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlQueryClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("QueryText");
    }

    [Fact(DisplayName = "Update с пустым QueryText → ValidationException")]
    public async Task Update_EmptyQueryText_ThrowsValidationException()
    {
        // Arrange
        var created = await CreateSqlQueryAsync();
        var request = new UpdateSqlQueryRequest("", false, false);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlQueryClient.UpdateAsync(created.Id, request));

        // Assert
        ex.Errors.ShouldContainKey("QueryText");
    }

    [Fact(DisplayName = "Delete запроса, используемого заданием → ConflictException")]
    public async Task Delete_QueryUsedBySqlTask_ThrowsConflictException()
    {
        // Arrange — создаём SqlQuery, затем SqlTask, ссылающийся на него
        var sqlQuery = await CreateSqlQueryAsync("SELECT * FROM tasks");

        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));

        await SeedSqlTaskAsync(targetDb.Id, topic.Id, sqlQuery.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlQueryClient.DeleteAsync(sqlQuery.Id));
    }

    [Fact(DisplayName = "Delete запроса, используемого попыткой (но не заданием) → ConflictException")]
    public async Task Delete_QueryUsedByAttempt_ThrowsConflictException()
    {
        // Arrange:
        //   sqlQueryToDelete — используется в Attempt.QueryId, НЕ используется в SqlTask.SqlQueryId
        //   sqlQueryForTask  — используется в SqlTask.SqlQueryId (другой запрос)
        var sqlQueryToDelete = await CreateSqlQueryAsync("SELECT * FROM attempt_query");
        var sqlQueryForTask = await CreateSqlQueryAsync("SELECT * FROM task_etalon");

        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));

        var taskId = await SeedSqlTaskAsync(targetDb.Id, topic.Id, sqlQueryForTask.Id);
        await SeedAttemptAsync(taskId, sqlQueryToDelete.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlQueryClient.DeleteAsync(sqlQueryToDelete.Id));
    }
}
