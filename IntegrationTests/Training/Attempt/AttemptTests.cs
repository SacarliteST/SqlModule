using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.Common;
using SQLModule.Host.Features.Training.Attempts;
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

    private async Task<(Guid taskId, Guid queryId)> CreatePrerequisitesAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var topic = await TopicClient.CreateAsync(
            new CreateTopicRequest("Topic_" + Guid.NewGuid(), null));

        var sqlQuery = await SqlQueryClient.CreateAsync(
            new CreateSqlQueryRequest(targetDb.Id, "SELECT 1", false, false));

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = DomainSqlTask.Create(
            targetDb.Id, topic.Id, sqlQuery.Id, "Task_" + Guid.NewGuid(), "Text", 1);
        db.SqlTasks.Add(task);
        await db.SaveChangesAsync();

        return (task.Id, sqlQuery.Id);
    }

    private async Task<AttemptResponse> CreateAttemptAsync(Guid taskId, Guid queryId, bool isSuccess = true)
    {
        var start = DateTimeOffset.UtcNow.AddSeconds(-10);
        var end = DateTimeOffset.UtcNow;
        return await AttemptClient.CreateAsync(
            new CreateAttemptRequest(isSuccess, start, end, taskId, queryId));
    }

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

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = Domain.DbmsCatalog.DbmsDictionary.Create(
            "Test_" + Guid.NewGuid(), "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);
        await db.SaveChangesAsync();
        return dbms.Id;
    }

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
        found.Id.ShouldBe(created.Id);
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
        updated.EndAttempt.ShouldBe(newEnd, TimeSpan.FromSeconds(1));
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

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await AttemptClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => AttemptClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => AttemptClient.UpdateAsync(Guid.NewGuid(),
                new UpdateAttemptRequest(true, DateTimeOffset.UtcNow)));
    }

    [Fact(DisplayName = "Create с несуществующим TaskId → ConflictException")]
    public async Task Create_UnknownTaskId_ThrowsConflictException()
    {
        // Arrange
        var (_, queryId) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => AttemptClient.CreateAsync(
                new CreateAttemptRequest(true, start, DateTimeOffset.UtcNow, Guid.NewGuid(), queryId)));
    }

    [Fact(DisplayName = "Create с несуществующим QueryId → ConflictException")]
    public async Task Create_UnknownQueryId_ThrowsConflictException()
    {
        // Arrange
        var (taskId, _) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => AttemptClient.CreateAsync(
                new CreateAttemptRequest(true, start, DateTimeOffset.UtcNow, taskId, Guid.NewGuid())));
    }

    [Fact(DisplayName = "Create с EndAttempt < StartAttempt → ValidationException")]
    public async Task Create_EndBeforeStart_ThrowsValidationException()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var start = DateTimeOffset.UtcNow;
        var end = start.AddSeconds(-1);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => AttemptClient.CreateAsync(new CreateAttemptRequest(true, start, end, taskId, queryId)));

        // Assert
        ex.Errors.ShouldContainKey("EndAttempt");
    }

    [Fact(DisplayName = "Update с EndAttempt < entity.StartAttempt → ValidationException (422)")]
    public async Task Update_EndBeforeEntityStart_ThrowsValidationException()
    {
        // Arrange
        var (taskId, queryId) = await CreatePrerequisitesAsync();
        var created = await CreateAttemptAsync(taskId, queryId);
        var endBeforeStart = created.StartAttempt.AddSeconds(-1);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => AttemptClient.UpdateAsync(created.Id, new UpdateAttemptRequest(true, endBeforeStart)));

        // Assert
        ex.Problem!.Title.ShouldBe(AttemptErrors.InvalidTimeRange.Code);
    }
}
