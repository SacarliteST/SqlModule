using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.Attempt;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Common;
using SQLModule.IntegrationTests.infrastructure;
using DomainAttempt = SQLModule.Domain.Training.Attempt;
using DomainSqlTask = SQLModule.Domain.Training.SqlTask;

namespace SQLModule.IntegrationTests.Training.Attempt;

/// <summary>
/// Интеграционные тесты CRUD-операций для <see cref="IAttemptClient"/>.
/// Каждый тест независим: создаёт собственные данные через вспомогательные методы.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class AttemptTests : ApiTestBase
{
    private readonly TestApplication app;

    public AttemptTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    /// <summary>
    /// Создаёт полный набор предусловий (DBMS → TargetDb → Topic → SqlQuery → SqlTask)
    /// и возвращает (taskId, queryId), пригодных для создания Attempt.
    /// </summary>
    private async Task<(Guid taskId, Guid queryId)> CreatePrerequisitesAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));

        var sqlQuery = await SqlQueryClient.CreateAsync(
            new CreateSqlQueryRequest("SELECT 1", false, false));

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = DomainSqlTask.Create(
            targetDb.Id, topic.Id, sqlQuery.Id, "Task_" + Guid.NewGuid(), "Text", 1);
        db.SqlTasks.Add(task);
        await db.SaveChangesAsync();

        return (task.Id, sqlQuery.Id);
    }

    /// <summary>Создаёт Attempt через API с корректными предусловиями.</summary>
    private async Task<AttemptResponse> CreateAttemptAsync(
        Guid taskId, Guid queryId, bool isSuccess = true)
    {
        var start = DateTimeOffset.UtcNow.AddSeconds(-10);
        var end = DateTimeOffset.UtcNow;
        return await AttemptClient.CreateAsync(
            new CreateAttemptRequest(isSuccess, start, end, taskId, queryId));
    }

    /// <summary>Создаёт Attempt напрямую через AppDbContext (для тестов удаления).</summary>
    private async Task<Guid> SeedAttemptAsync(Guid taskId, Guid queryId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = DomainAttempt.Create(
            Guid.NewGuid(), true,
            DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow,
            taskId, queryId);
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync();
        return attempt.Id;
    }

    /// <summary>Создаёт DbmsDictionary напрямую через AppDbContext.</summary>
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

    // happy-path

    [Fact(DisplayName = "Create → возвращает AttemptResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow.AddSeconds(-10);
        var end = DateTimeOffset.UtcNow;
        var request = new CreateAttemptRequest(true, start, end, taskId, queryId);

        // Act
        var response = await AttemptClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.IsSuccess.ShouldBeTrue();
        response.TaskId.ShouldBe(taskId);
        response.QueryId.ShouldBe(queryId);
    }

    [Fact(DisplayName = "Create → UserId в ответе равен SystemUser.Id (нет JWT)")]
    public async Task Create_NoJwt_UserIdEqualsSystemUserId()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();

        // Act
        var response = await CreateAttemptAsync(taskId, queryId);

        // Assert
        response.UserId.ShouldBe(SystemUser.Id);
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную попытку")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var created = await CreateAttemptAsync(taskId, queryId);

        // Act
        var found = await AttemptClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.TaskId.ShouldBe(taskId);
        found.QueryId.ShouldBe(queryId);
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную попытку")]
    public async Task GetAll_ContainsCreatedAttempt()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var created = await CreateAttemptAsync(taskId, queryId);

        // Act
        var page = await AttemptClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(a => a.Id == created.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var created = await CreateAttemptAsync(taskId, queryId, isSuccess: false);
        var newEnd = created.EndAttempt.AddMinutes(5);

        // Act
        await AttemptClient.UpdateAsync(created.Id, new UpdateAttemptRequest(true, newEnd));

        // Assert
        var updated = await AttemptClient.GetByIdAsync(created.Id);
        updated!.IsSuccess.ShouldBeTrue();
        updated.EndAttempt.ShouldBe(newEnd);
    }

    [Fact(DisplayName = "Delete → попытка больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var created = await CreateAttemptAsync(taskId, queryId);

        // Act
        await AttemptClient.DeleteAsync(created.Id);

        // Assert
        var found = await AttemptClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    // негатив

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var result = await AttemptClient.GetByIdAsync(unknownId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act + Assert
        await Should.NotThrowAsync(() => AttemptClient.DeleteAsync(unknownId));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        var request = new UpdateAttemptRequest(true, DateTimeOffset.UtcNow);

        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => AttemptClient.UpdateAsync(unknownId, request));
    }

    [Fact(DisplayName = "Create с несуществующим TaskId → ConflictException")]
    public async Task Create_UnknownTaskId_ThrowsConflictException()
    {
        // Arrange
        var (_, queryId) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);
        var request = new CreateAttemptRequest(true, start, DateTimeOffset.UtcNow, Guid.NewGuid(), queryId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => AttemptClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с несуществующим QueryId → ConflictException")]
    public async Task Create_UnknownQueryId_ThrowsConflictException()
    {
        // Arrange
        var (taskId, _) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);
        var request = new CreateAttemptRequest(true, start, DateTimeOffset.UtcNow, taskId, Guid.NewGuid());

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => AttemptClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с EndAttempt < StartAttempt → ValidationException")]
    public async Task Create_EndBeforeStart_ThrowsValidationException()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddSeconds(-1);
        var request = new CreateAttemptRequest(true, start, end, taskId, queryId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => AttemptClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("EndAttempt");
    }

    [Fact(DisplayName = "Update с EndAttempt < entity.StartAttempt → ConflictException")]
    public async Task Update_EndBeforeEntityStart_ThrowsConflictException()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var created = await CreateAttemptAsync(taskId, queryId);

        // EndAttempt устанавливаем раньше StartAttempt, которое уже зафиксировано в БД
        var endBeforeStart = created.StartAttempt.AddSeconds(-1);
        var request = new UpdateAttemptRequest(true, endBeforeStart);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => AttemptClient.UpdateAsync(created.Id, request));
    }
}
