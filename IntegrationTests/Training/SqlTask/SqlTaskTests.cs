using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;
using DomainAttempt = SQLModule.Domain.Training.Attempt;

namespace SQLModule.IntegrationTests.Training.SqlTask;

[Collection(IntegrationTestCollection.Name)]
public sealed class SqlTaskTests : ApiTestBase
{
    private readonly TestApplication app;

    public SqlTaskTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DbmsDictionary.Create(
            "Test_" + Guid.NewGuid(), "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);
        await db.SaveChangesAsync();
        return dbms.Id;
    }

    private async Task<Guid> CreateTargetDbAsync(Guid dbmsId)
        => (await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false))).Id;

    private async Task<Guid> CreateTopicAsync()
        => (await TopicClient.CreateAsync(new CreateTopicRequest("Topic_" + Guid.NewGuid(), null))).Id;

    private async Task<Guid> CreateSqlQueryAsync(Guid targetDbId)
        => (await SqlQueryClient.CreateAsync(new CreateSqlQueryRequest(targetDbId, "SELECT 1", false, false))).Id;

    private async Task<SqlTaskResponse> CreateSqlTaskAsync(
        Guid targetDbId, Guid topicId, Guid sqlQueryId, string taskName = "Task")
        => await SqlTaskClient.CreateAsync(
            new CreateSqlTaskRequest(targetDbId, topicId, sqlQueryId, taskName, "Текст задания", 1));

    private async Task SeedAttemptAsync(Guid taskId, Guid sqlQueryId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = DomainAttempt.Create(
            Guid.NewGuid(), true,
            DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow,
            taskId, sqlQueryId);
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync();
    }

    [Fact(DisplayName = "Create → возвращает SqlTaskResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var request = new CreateSqlTaskRequest(targetDbId, topicId, sqlQueryId, "My Task", "Описание", 3);

        // Act
        var response = await SqlTaskClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.TargetDbId.ShouldBe(targetDbId);
        response.TopicId.ShouldBe(topicId);
        response.SqlQueryId.ShouldBe(sqlQueryId);
        response.TaskName.ShouldBe("My Task");
        response.DifficultyLevel.ShouldBe((short)3);
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданное задание")]
    public async Task GetById_ExistingId_ReturnsSqlTask()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(targetDbId, topicId, sqlQueryId, "GetMe");

        // Act
        var found = await SqlTaskClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.TaskName.ShouldBe("GetMe");
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await SqlTaskClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetAll → страница содержит созданное задание")]
    public async Task GetAll_ContainsCreatedTask()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(targetDbId, topicId, sqlQueryId);

        // Act
        var page = await SqlTaskClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(t => t.Id == created.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(targetDbId, topicId, sqlQueryId, "OldName");

        // Act
        await SqlTaskClient.UpdateAsync(created.Id, new UpdateSqlTaskRequest("NewName", "Новый текст", 5));

        // Assert
        var updated = await SqlTaskClient.GetByIdAsync(created.Id);
        updated!.TaskName.ShouldBe("NewName");
        updated.DifficultyLevel.ShouldBe((short)5);
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => SqlTaskClient.UpdateAsync(Guid.NewGuid(), new UpdateSqlTaskRequest("Name", "Text", 1)));
    }

    [Fact(DisplayName = "Delete → задание больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(targetDbId, topicId, sqlQueryId, "ToDelete");

        // Act
        await SqlTaskClient.DeleteAsync(created.Id);

        // Assert
        var found = await SqlTaskClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => SqlTaskClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Create с несуществующим TargetDbId → ConflictException")]
    public async Task Create_NonExistentTargetDbId_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(Guid.NewGuid(), topicId, sqlQueryId, "Task", "Text", 1)));
    }

    [Fact(DisplayName = "Create с несуществующим TopicId → ConflictException")]
    public async Task Create_NonExistentTopicId_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(targetDbId, Guid.NewGuid(), sqlQueryId, "Task", "Text", 1)));
    }

    [Fact(DisplayName = "Create с несуществующим SqlQueryId → ConflictException")]
    public async Task Create_NonExistentSqlQueryId_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(targetDbId, topicId, Guid.NewGuid(), "Task", "Text", 1)));
    }

    [Fact(DisplayName = "Create с пустым TaskName → ValidationException")]
    public async Task Create_EmptyTaskName_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "Text", 1)));

        // Assert
        ex.Errors.ShouldContainKey("TaskName");
    }

    [Fact(DisplayName = "Create с DifficultyLevel вне диапазона → ValidationException")]
    public async Task Create_DifficultyLevelOutOfRange_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Task", "Text", 6)));

        // Assert
        ex.Errors.ShouldContainKey("DifficultyLevel");
    }

    [Fact(DisplayName = "Update с пустым TaskName → ValidationException")]
    public async Task Update_EmptyTaskName_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.UpdateAsync(Guid.NewGuid(), new UpdateSqlTaskRequest("", "Text", 1)));

        // Assert
        ex.Errors.ShouldContainKey("TaskName");
    }

    [Fact(DisplayName = "Delete задания с попытками → ConflictException")]
    public async Task Delete_TaskWithAttempts_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(targetDbId, topicId, sqlQueryId, "WithAttempts");
        await SeedAttemptAsync(created.Id, sqlQueryId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.DeleteAsync(created.Id));
    }
}
