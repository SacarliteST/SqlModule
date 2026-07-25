using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.IntegrationTests.infrastructure;
using DomainSqlTask = SQLModule.Domain.Training.SqlTask;

namespace SQLModule.IntegrationTests.Training.SqlQuery;

[Collection(IntegrationTestCollection.Name)]
public sealed class SqlQueryTests : ApiTestBase
{

    public SqlQueryTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private async Task<Guid> SeedTargetDbIdAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        return targetDb.Id;
    }

    private async Task<SqlQueryResponse> CreateSqlQueryAsync(string queryText = "SELECT 1")
    {
        var targetDbId = await SeedTargetDbIdAsync();
        return await SqlQueryClient.CreateAsync(new CreateSqlQueryRequest(targetDbId, queryText, false, false));
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

    private async Task<Guid> SeedSqlTaskAsync(Guid targetDbId, Guid topicId, Guid sqlQueryId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = DomainSqlTask.Create(topicId, sqlQueryId, "Task_" + Guid.NewGuid(), "Text", 1);
        db.SqlTasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    [Fact(DisplayName = "Create → возвращает SqlQueryResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var targetDbId = await SeedTargetDbIdAsync();
        var request = new CreateSqlQueryRequest(targetDbId, "SELECT id FROM users", true, false);

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

    [Fact(DisplayName = "Validate → возвращает preview и не сохраняет изменения")]
    public async Task Validate_ValidQuery_ReturnsPreviewWithoutPersistence()
    {
        // Arrange
        var targetDbId = await SeedTargetDbIdAsync();
        var existing = await SqlQueryClient.CreateAsync(
            new CreateSqlQueryRequest(targetDbId, "SELECT 1", false, false));

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fakeSandbox = (global::SQLModule.Web.Common.Isolated.FakeSandboxExecutor)
            scope.ServiceProvider.GetRequiredService<global::SQLModule.Sandbox.ISandboxExecutor>();
        var countBefore = db.SqlQueries.Count();
        fakeSandbox.OverrideRun = global::SQLModule.Common.Results.Result<global::SQLModule.Sandbox.QueryResultSet>.Success(
            new global::SQLModule.Sandbox.QueryResultSet(
                true,
                null,
                ["id", "name"],
                [["1", "Ivan"], ["2", "Anna"]],
                2,
                37));

        try
        {
            // Act
            var response = await SqlQueryClient.ValidateAsync(
                new ValidateSqlQueryRequest(targetDbId, "SELECT id, name FROM users"));

            // Assert
            response.IsValid.ShouldBeTrue();
            response.Columns.ShouldBe(["id", "name"]);
            response.SampleRows.Count.ShouldBe(2);
            response.RowCount.ShouldBe(2);
            response.ExecutionTimeMs.ShouldBe(37);
            response.ValidatedAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
            db.SqlQueries.Count().ShouldBe(countBefore);

            var unchanged = await SqlQueryClient.GetByIdAsync(existing.Id);
            unchanged!.QueryText.ShouldBe("SELECT 1");
        }
        finally
        {
            fakeSandbox.OverrideRun = null;
        }
    }

    [Fact(DisplayName = "Validate с SQL-ошибкой → ValidationException без сохранения")]
    public async Task Validate_InvalidQuery_ThrowsValidationExceptionWithoutPersistence()
    {
        // Arrange
        var targetDbId = await SeedTargetDbIdAsync();
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fakeSandbox = (global::SQLModule.Web.Common.Isolated.FakeSandboxExecutor)
            scope.ServiceProvider.GetRequiredService<global::SQLModule.Sandbox.ISandboxExecutor>();
        var countBefore = db.SqlQueries.Count();
        fakeSandbox.OverrideRun = global::SQLModule.Common.Results.Result<global::SQLModule.Sandbox.QueryResultSet>.Success(
            new global::SQLModule.Sandbox.QueryResultSet(
                false,
                "relation users does not exist",
                [],
                [],
                0,
                12));

        try
        {
            // Act + Assert
            await Should.ThrowAsync<ValidationException>(
                () => SqlQueryClient.ValidateAsync(
                    new ValidateSqlQueryRequest(targetDbId, "SELECT * FROM users")));
            db.SqlQueries.Count().ShouldBe(countBefore);
        }
        finally
        {
            fakeSandbox.OverrideRun = null;
        }
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

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await SqlQueryClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => SqlQueryClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => SqlQueryClient.UpdateAsync(Guid.NewGuid(), new UpdateSqlQueryRequest("SELECT 1", false, false)));
    }

    [Fact(DisplayName = "Create с пустым QueryText → ValidationException")]
    public async Task Create_EmptyQueryText_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlQueryClient.CreateAsync(new CreateSqlQueryRequest(Guid.NewGuid(), "", false, false)));

        // Assert
        ex.Errors.ShouldContainKey("QueryText");
    }

    [Fact(DisplayName = "Update с пустым QueryText → ValidationException")]
    public async Task Update_EmptyQueryText_ThrowsValidationException()
    {
        // Arrange
        var created = await CreateSqlQueryAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlQueryClient.UpdateAsync(created.Id, new UpdateSqlQueryRequest("", false, false)));

        // Assert
        ex.Errors.ShouldContainKey("QueryText");
    }

    [Fact(DisplayName = "Delete запроса, используемого заданием → ConflictException")]
    public async Task Delete_QueryUsedBySqlTask_ThrowsConflictException()
    {
        // Arrange
        var sqlQuery = await CreateSqlQueryAsync("SELECT * FROM tasks");

        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));

        await SeedSqlTaskAsync(targetDb.Id, topic.Id, sqlQuery.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlQueryClient.DeleteAsync(sqlQuery.Id));
    }

}
