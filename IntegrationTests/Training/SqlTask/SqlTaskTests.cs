using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.SqlTask;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.SqlTask;

/// <summary>
/// Интеграционные тесты CRUD-операций для <see cref="ISqlTaskClient"/>.
/// Каждый тест независим: создаёт собственные данные через вспомогательные методы.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SqlTaskTests : ApiTestBase
{
    private readonly TestApplication app;

    public SqlTaskTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    /// <summary>Создаёт запись СУБД-справочника и возвращает её Id.</summary>
    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        var response = await HttpClient.PostAsJsonAsync("api/v1/dbms-dictionaries", new
        {
            DbmsName = "Test_" + Guid.NewGuid(),
            DbmsSystemName = "test",
            DockerImage = "postgres:latest",
            DefaultPort = 5432,
            EnvUserKey = "POSTGRES_USER",
            EnvPasswordKey = "POSTGRES_PASSWORD",
            EnvDatabaseKey = "POSTGRES_DB",
            ExtraEnvConfig = (string?)null,
            DefaultDatabase = "testdb",
            DefaultUsername = "user",
            DefaultPassword = "pass"
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return json.GetProperty("id").GetGuid();
    }

    /// <summary>Создаёт TargetDb и возвращает её Id.</summary>
    private async Task<Guid> CreateTargetDbAsync(Guid dbmsId)
    {
        var result = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        return result.Id;
    }

    /// <summary>Создаёт тему и возвращает её Id.</summary>
    private async Task<Guid> CreateTopicAsync()
    {
        var result = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));
        return result.Id;
    }

    /// <summary>
    /// Создаёт SQL-запрос напрямую через AppDbContext (HTTP-API для SqlQuery ещё не реализовано).
    /// </summary>
    private async Task<Guid> CreateSqlQueryAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = SqlQuery.Create("SELECT 1", false, false);
        db.SqlQueries.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    /// <summary>Создаёт SQL-задание с готовыми FK и возвращает ответ сервера.</summary>
    private async Task<SqlTaskResponse> CreateSqlTaskAsync(
        Guid targetDbId, Guid topicId, Guid sqlQueryId, string taskName = "Task")
        => await SqlTaskClient.CreateAsync(
            new CreateSqlTaskRequest(targetDbId, topicId, sqlQueryId, taskName, "Текст задания", 1));

    /// <summary>Создаёт попытку выполнения задания напрямую через AppDbContext.</summary>
    private async Task SeedAttemptAsync(Guid taskId, Guid sqlQueryId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = Attempt.Create(
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
        var sqlQueryId = await CreateSqlQueryAsync();
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

    [Fact(DisplayName = "Create с несуществующим TargetDbId → ConflictException")]
    public async Task Create_NonExistentTargetDbId_ThrowsConflictException()
    {
        // Arrange
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync();
        var request = new CreateSqlTaskRequest(Guid.NewGuid(), topicId, sqlQueryId, "Task", "Text", 1);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с несуществующим TopicId → ConflictException")]
    public async Task Create_NonExistentTopicId_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var sqlQueryId = await CreateSqlQueryAsync();
        var request = new CreateSqlTaskRequest(targetDbId, Guid.NewGuid(), sqlQueryId, "Task", "Text", 1);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с несуществующим SqlQueryId → ConflictException")]
    public async Task Create_NonExistentSqlQueryId_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var request = new CreateSqlTaskRequest(targetDbId, topicId, Guid.NewGuid(), "Task", "Text", 1);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с пустым TaskName → ValidationException")]
    public async Task Create_EmptyTaskName_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateSqlTaskRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "Text", 1);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("TaskName");
    }

    [Fact(DisplayName = "Create с DifficultyLevel вне диапазона → ValidationException")]
    public async Task Create_DifficultyLevelOutOfRange_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateSqlTaskRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Task", "Text", 6);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("DifficultyLevel");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданное задание")]
    public async Task GetById_ExistingId_ReturnsSqlTask()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync();
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
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var result = await SqlTaskClient.GetByIdAsync(unknownId);

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
        var sqlQueryId = await CreateSqlQueryAsync();
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
        var sqlQueryId = await CreateSqlQueryAsync();
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
        // Arrange
        var unknownId = Guid.NewGuid();
        var request = new UpdateSqlTaskRequest("Name", "Text", 1);

        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => SqlTaskClient.UpdateAsync(unknownId, request));
    }

    [Fact(DisplayName = "Update с пустым TaskName → ValidationException")]
    public async Task Update_EmptyTaskName_ThrowsValidationException()
    {
        // Arrange
        var request = new UpdateSqlTaskRequest("", "Text", 1);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.UpdateAsync(Guid.NewGuid(), request));

        // Assert
        ex.Errors.ShouldContainKey("TaskName");
    }

    [Fact(DisplayName = "Delete → задание больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync();
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
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act + Assert
        await Should.NotThrowAsync(() => SqlTaskClient.DeleteAsync(unknownId));
    }

    [Fact(DisplayName = "Delete задания с попытками → ConflictException")]
    public async Task Delete_TaskWithAttempts_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync();
        var created = await CreateSqlTaskAsync(targetDbId, topicId, sqlQueryId, "WithAttempts");
        await SeedAttemptAsync(created.Id, sqlQueryId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.DeleteAsync(created.Id));
    }
}
