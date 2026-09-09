using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Isolated;
using SQLModule.Web.Common.Sandbox;
using DomainAttempt = SQLModule.Domain.Training.Attempt;

namespace SQLModule.IntegrationTests.Training.SqlTask;

[Collection(IntegrationTestCollection.Name)]
public sealed class SqlTaskTests : ApiTestBase
{

    public SqlTaskTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private FakeSandboxExecutor GetFakeExecutor() =>
        (FakeSandboxExecutor)App.Services.GetRequiredService<ISandboxExecutor>();

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = App.Services.CreateScope();
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
            new Contracts.Schema.TargetDb.CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false))).Id;

    private async Task<Guid> CreateTopicAsync()
        => (await TopicClient.CreateAsync(new CreateTopicRequest("Topic_" + Guid.NewGuid(), null))).Id;

    private async Task<Guid> CreateSqlQueryAsync(Guid targetDbId)
        => (await SqlQueryClient.CreateAsync(new CreateSqlQueryRequest(targetDbId, "SELECT 1", false, false))).Id;

    private async Task<SqlTaskResponse> CreateSqlTaskAsync(
        Guid topicId,
        Guid referenceSourceId,
        string taskName = "Task")
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var targetDbId = await db.SqlQueries
            .Where(x => x.Id == referenceSourceId)
            .Select(x => (Guid?)x.TargetDbId)
            .FirstOrDefaultAsync() ?? referenceSourceId;

        return await SqlTaskClient.CreateAsync(
            new CreateSqlTaskRequest(
                topicId,
                taskName,
                "Текст задания",
                1,
                Reference(targetDbId)));
    }

    private static ReferenceQueryRequest Reference(
        Guid targetDbId,
        string queryText = "SELECT 1",
        bool strictColumnOrder = false,
        bool strictRowOrder = false)
        => new(targetDbId, queryText, strictColumnOrder, strictRowOrder);

    private async Task SeedAttemptAsync(Guid taskId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var attempt = DomainAttempt.Record(
            Guid.NewGuid(), taskId, "SELECT 1",
            Domain.Training.ExecutionStatus.Succeeded, true, Domain.Training.CheckReason.Ok,
            0, 0, null,
            DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow);
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
        var request = new CreateSqlTaskRequest(
            topicId,
            "My Task",
            "Описание",
            3,
            Reference(targetDbId));

        // Act
        var response = await SqlTaskClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.TopicId.ShouldBe(topicId);
        response.SqlQueryId.ShouldNotBe(Guid.Empty);
        response.TaskName.ShouldBe("My Task");
        response.DifficultyLevel.ShouldBe((short)3);
        response.PublicationStatus.ShouldBe(PublicationStatus.Draft);
        response.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        response.UpdatedAt.ShouldBeGreaterThanOrEqualTo(response.CreatedAt);
        response.CreatedById.ShouldNotBe(Guid.Empty);
        response.UpdatedById.ShouldNotBe(Guid.Empty);
        response.CreatedByName.ShouldBe("Test Teacher");
        response.UpdatedByName.ShouldBe("Test Teacher");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданное задание")]
    public async Task GetById_ExistingId_ReturnsSqlTask()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(topicId, sqlQueryId, "GetMe");

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
        var created = await CreateSqlTaskAsync(topicId, sqlQueryId);

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
        var created = await CreateSqlTaskAsync(topicId, sqlQueryId, "OldName");

        // Act
        await SqlTaskClient.UpdateAsync(created.Id, new UpdateSqlTaskRequest("NewName", "Новый текст", 5));

        // Assert
        var updated = await SqlTaskClient.GetByIdAsync(created.Id);
        updated!.TaskName.ShouldBe("NewName");
        updated.DifficultyLevel.ShouldBe((short)5);
        updated.PublicationStatus.ShouldBe(PublicationStatus.Draft);
    }

    [Fact(DisplayName = "Create → атомарно создаёт собственный проверенный эталон")]
    public async Task Create_WithNestedReference_PersistsOwnedReference()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();

        // Act
        var task = await SqlTaskClient.CreateAsync(
            new CreateSqlTaskRequest(
                topicId,
                "Nested reference",
                "Text",
                2,
                Reference(targetDbId, "SELECT 42", true, false)));

        // Assert
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reference = await db.SqlQueries.SingleAsync(x => x.Id == task.SqlQueryId);
        reference.TargetDbId.ShouldBe(targetDbId);
        reference.QueryText.ShouldBe("SELECT 42");
        reference.StrictColumnOrder.ShouldBeTrue();
        reference.StrictRowOrder.ShouldBeFalse();
        reference.ExpectedResult.ShouldNotBeNullOrWhiteSpace();
        (await db.SqlTasks.CountAsync(x => x.SqlQueryId == reference.Id)).ShouldBe(1);
    }

    [Fact(DisplayName = "Update без ReferenceQuery → текущий эталон не изменяется")]
    public async Task Update_WithoutReference_PreservesReference()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);

        // Act
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest("Renamed", task.TaskText, task.DifficultyLevel));

        // Assert
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reference = await db.SqlQueries.SingleAsync(x => x.Id == task.SqlQueryId);
        reference.TargetDbId.ShouldBe(targetDbId);
        reference.QueryText.ShouldBe("SELECT 1");
    }

    [Fact(DisplayName = "Отдельный endpoint эталона → обновляет только текущий эталон")]
    public async Task Update_WithCompleteReference_UpdatesOwnedReference()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var initialTargetDbId = await CreateTargetDbAsync(dbmsId);
        var newTargetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, initialTargetDbId);

        // Act
        var response = await SqlTaskClient.UpdateReferenceQueryAsync(
            task.Id,
            new UpdateTaskReferenceQueryRequest(newTargetDbId, "SELECT 2", true, true));

        // Assert
        response.SqlText.ShouldBe("SELECT 2");
        response.IsRequiredColumnOrder.ShouldBeTrue();
        response.IsRequiredRowOrder.ShouldBeTrue();
        response.TargetDb.TargetDbId.ShouldBe(newTargetDbId);
        response.TargetDb.DbName.ShouldNotBeNullOrWhiteSpace();
        response.TargetDb.DbmsName.ShouldNotBeNullOrWhiteSpace();

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reference = await db.SqlQueries.SingleAsync(x => x.Id == task.SqlQueryId);
        reference.TargetDbId.ShouldBe(newTargetDbId);
        reference.QueryText.ShouldBe("SELECT 2");
        reference.StrictColumnOrder.ShouldBeTrue();
        reference.StrictRowOrder.ShouldBeTrue();
    }

    [Fact(DisplayName = "Неполный эталон → ошибки валидации полей запроса")]
    public async Task Update_WithPartialReference_ReturnsNestedValidationErrors()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.UpdateReferenceQueryAsync(
                Guid.NewGuid(),
                new UpdateTaskReferenceQueryRequest(null, null, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("TargetDbId");
        ex.Errors.ShouldContainKey("QueryText");
        ex.Errors.ShouldContainKey("StrictColumnOrder");
        ex.Errors.ShouldContainKey("StrictRowOrder");
    }

    [Fact(DisplayName = "Update задания с некорректными nullable-полями → единый 422 со всеми путями")]
    public async Task Update_WithInvalidNullableFields_ReturnsStructuredValidationErrors()
    {
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.UpdateAsync(
                Guid.NewGuid(),
                new UpdateSqlTaskRequest(
                    null,
                    null,
                    null,
                    (PublicationStatus)999,
                    Guid.Empty)));

        ex.StatusCode.ShouldBe(422);
        ex.Errors.ShouldContainKey("TaskName");
        ex.Errors.ShouldContainKey("TaskText");
        ex.Errors.ShouldContainKey("DifficultyLevel");
        ex.Errors.ShouldContainKey("PublicationStatus");
        ex.Errors.ShouldContainKey("TopicId");
    }

    [Fact(DisplayName = "Update эталона Published-задания → ConflictException")]
    public async Task UpdateReference_PublishedTask_ThrowsConflictException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);
        await SqlTaskClient.PublishAsync(task.Id);

        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateReferenceQueryAsync(
                task.Id,
                new UpdateTaskReferenceQueryRequest(targetDbId, "SELECT 2", false, false)));
    }

    [Fact(DisplayName = "Update эталона Draft-задания с попытками → ConflictException")]
    public async Task UpdateReference_DraftWithAttempts_ThrowsConflictException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);
        await SeedAttemptAsync(task.Id);

        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateReferenceQueryAsync(
                task.Id,
                new UpdateTaskReferenceQueryRequest(targetDbId, "SELECT 2", false, false)));
    }

    [Fact(DisplayName = "Update эталона Archived-задания → ConflictException")]
    public async Task UpdateReference_ArchivedTask_ThrowsConflictException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest(
                task.TaskName,
                task.TaskText,
                task.DifficultyLevel,
                PublicationStatus.Archived));

        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateReferenceQueryAsync(
                task.Id,
                new UpdateTaskReferenceQueryRequest(targetDbId, "SELECT 2", false, false)));
    }

    [Fact(DisplayName = "Update темы Draft без попыток → тема изменена, эталон сохранён")]
    public async Task UpdateLinks_DraftWithoutAttempts_PersistsLinks()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var initialTopicId = await CreateTopicAsync();
        var newTopicId = await CreateTopicAsync();
        var initialQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(initialTopicId, initialQueryId);

        // Act
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest(
                task.TaskName,
                task.TaskText,
                task.DifficultyLevel,
                TopicId: newTopicId));

        // Assert
        var updated = await SqlTaskClient.GetByIdAsync(task.Id);
        updated!.TopicId.ShouldBe(newTopicId);
        updated.SqlQueryId.ShouldBe(task.SqlQueryId);
    }

    [Fact(DisplayName = "Update связей Published → ConflictException")]
    public async Task UpdateLinks_PublishedTask_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var newTopicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);
        var published = await SqlTaskClient.PublishAsync(task.Id);

        // Act + Assert
        var ex = await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateAsync(
                task.Id,
                new UpdateSqlTaskRequest(
                    published.TaskName,
                    published.TaskText,
                    published.DifficultyLevel,
                    PublicationStatus.Published,
                    TopicId: newTopicId)));

        ex.Problem!.Title.ShouldBe("Конфликт состояния");
        ex.Problem.Code.ShouldBe("SqlTask.LinksChangeRequiresDraft");
        ex.Problem.Detail.ShouldNotBeNullOrWhiteSpace();
        ex.Problem.Errors.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Update связей Archived → ConflictException")]
    public async Task UpdateLinks_ArchivedTask_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var newTopicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest(
                task.TaskName,
                task.TaskText,
                task.DifficultyLevel,
                PublicationStatus.Archived));

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateAsync(
                task.Id,
                new UpdateSqlTaskRequest(
                    task.TaskName,
                    task.TaskText,
                    task.DifficultyLevel,
                    PublicationStatus.Archived,
                    TopicId: newTopicId)));
    }

    [Fact(DisplayName = "Update связей Draft с попытками → ConflictException")]
    public async Task UpdateLinks_DraftWithAttempts_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var newTopicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);
        await SeedAttemptAsync(task.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateAsync(
                task.Id,
                new UpdateSqlTaskRequest(
                    task.TaskName,
                    task.TaskText,
                    task.DifficultyLevel,
                    TopicId: newTopicId)));
    }

    [Fact(DisplayName = "Update с несуществующей новой темой → ConflictException")]
    public async Task UpdateLinks_UnknownTopic_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateAsync(
                task.Id,
                new UpdateSqlTaskRequest(
                    task.TaskName,
                    task.TaskText,
                    task.DifficultyLevel,
                    TopicId: Guid.NewGuid())));
    }

    [Fact(DisplayName = "Update Published с текущими связями → изменения контента разрешены")]
    public async Task UpdateLinks_PublishedWithUnchangedLinks_AllowsContentUpdate()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);
        await SqlTaskClient.PublishAsync(task.Id);

        // Act
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest(
                "Updated published task",
                task.TaskText,
                task.DifficultyLevel,
                PublicationStatus.Published,
                topicId));

        // Assert
        var updated = await SqlTaskClient.GetByIdAsync(task.Id);
        updated!.TaskName.ShouldBe("Updated published task");
        updated.TopicId.ShouldBe(topicId);
        updated.SqlQueryId.ShouldBe(task.SqlQueryId);
    }

    [Fact(DisplayName = "TeacherDetails → возвращает агрегированную read-модель задания")]
    public async Task GetTeacherDetails_ExistingTask_ReturnsAggregate()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var draftTask = await CreateSqlTaskAsync(topicId, sqlQueryId, "Published task");
        var task = await SqlTaskClient.PublishAsync(draftTask.Id);

        var studentId = Guid.NewGuid();
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var physicalType = PhysicalType.Create(dbmsId, "integer");
            var table = MetaTable.Create(targetDbId, "employees", null);
            db.PhysicalTypes.Add(physicalType);
            db.MetaTables.Add(table);
            db.MetaAttributes.AddRange(
                MetaAttribute.Create(table.Id, physicalType.Id, "id", true, true, 0),
                MetaAttribute.Create(table.Id, physicalType.Id, "department_id", false, true, 1));
            db.Attempts.Add(DomainAttempt.Record(
                studentId,
                task.Id,
                "SELECT * FROM employees",
                ExecutionStatus.Succeeded,
                true,
                CheckReason.Ok,
                1,
                42,
                null,
                DateTimeOffset.UtcNow.AddSeconds(-1),
                DateTimeOffset.UtcNow,
                studentName: "Иван Петров"));
            await db.SaveChangesAsync();
        }

        // Act
        var details = await SqlTaskClient.GetTeacherDetailsAsync(task.Id);

        // Assert
        details.ShouldNotBeNull();
        details.TaskId.ShouldBe(task.Id);
        details.TaskName.ShouldBe("Published task");
        details.PublicationStatus.ShouldBe(PublicationStatus.Published);
        details.TargetDb.TargetDbId.ShouldBe(targetDbId);
        details.TargetDb.Tables.ShouldHaveSingleItem()
            .ShouldBe(new TeacherTaskTableResponse("employees", 2));
        details.AttemptsCount.ShouldBe(1);
        details.CanEditTask.ShouldBeTrue();
        details.CanEditReferenceQuery.ShouldBeFalse();
        details.ReferenceQueryEditRestriction.ShouldNotBeNull().ShouldContain("опубликованного");
        details.LastAttempts.ShouldHaveSingleItem().StudentName.ShouldBe("Иван Петров");
        details.LastAttempts[0].StudentId.ShouldBe(studentId);
        details.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        details.UpdatedAt.ShouldBeGreaterThanOrEqualTo(details.CreatedAt);
        details.CreatedById.ShouldNotBe(Guid.Empty);
        details.CreatedByName.ShouldBe("Test Teacher");
        details.LastAttempts[0].FinishedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
    }

    [Fact(DisplayName = "TeacherDetails Draft без попыток → эталон доступен для редактирования")]
    public async Task GetTeacherDetails_DraftWithoutAttempts_CanEditReference()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);

        var details = await SqlTaskClient.GetTeacherDetailsAsync(task.Id);

        details.ShouldNotBeNull();
        details.CanEditTask.ShouldBeTrue();
        details.CanEditReferenceQuery.ShouldBeTrue();
        details.ReferenceQueryEditRestriction.ShouldBeNull();
    }

    [Fact(DisplayName = "TeacherDetails Draft с попытками → возвращает причину запрета")]
    public async Task GetTeacherDetails_DraftWithAttempts_ReturnsRestriction()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);
        await SeedAttemptAsync(task.Id);

        var details = await SqlTaskClient.GetTeacherDetailsAsync(task.Id);

        details.ShouldNotBeNull();
        details.CanEditReferenceQuery.ShouldBeFalse();
        details.ReferenceQueryEditRestriction.ShouldNotBeNull().ShouldContain("попытки");
    }

    [Fact(DisplayName = "TeacherDetails Published → возвращает причину запрета")]
    public async Task GetTeacherDetails_Published_ReturnsRestriction()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);
        await SqlTaskClient.PublishAsync(task.Id);

        var details = await SqlTaskClient.GetTeacherDetailsAsync(task.Id);

        details.ShouldNotBeNull();
        details.CanEditReferenceQuery.ShouldBeFalse();
        details.ReferenceQueryEditRestriction.ShouldNotBeNull().ShouldContain("опубликованного");
    }

    [Fact(DisplayName = "TeacherDetails Archived → возвращает причину запрета")]
    public async Task GetTeacherDetails_Archived_ReturnsRestriction()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId);
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest(
                task.TaskName,
                task.TaskText,
                task.DifficultyLevel,
                PublicationStatus.Archived));

        var details = await SqlTaskClient.GetTeacherDetailsAsync(task.Id);

        details.ShouldNotBeNull();
        details.CanEditReferenceQuery.ShouldBeFalse();
        details.ReferenceQueryEditRestriction.ShouldNotBeNull().ShouldContain("архивного");
    }

    [Fact(DisplayName = "TeacherDetails несуществующего задания → null")]
    public async Task GetTeacherDetails_UnknownTask_ReturnsNull()
    {
        var details = await SqlTaskClient.GetTeacherDetailsAsync(Guid.NewGuid());
        details.ShouldBeNull();
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => SqlTaskClient.UpdateAsync(Guid.NewGuid(), new UpdateSqlTaskRequest("Name", "Text", 1)));
    }

    [Fact(DisplayName = "Update Draft в Published → ConflictException")]
    public async Task Update_DraftToPublished_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.UpdateAsync(
                task.Id,
                new UpdateSqlTaskRequest(
                    task.TaskName,
                    task.TaskText,
                    task.DifficultyLevel,
                    PublicationStatus.Published)));
    }

    [Fact(DisplayName = "Publish валидного Draft → Published")]
    public async Task Publish_ValidDraft_ReturnsPublishedTask()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);

        // Act
        var published = await SqlTaskClient.PublishAsync(task.Id);

        // Assert
        published.Id.ShouldBe(task.Id);
        published.PublicationStatus.ShouldBe(PublicationStatus.Published);
        (await SqlTaskClient.GetByIdAsync(task.Id))!.PublicationStatus
            .ShouldBe(PublicationStatus.Published);
    }

    [Fact(DisplayName = "Publish уже опубликованного задания → ConflictException")]
    public async Task Publish_AlreadyPublished_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);
        await SqlTaskClient.PublishAsync(task.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(() => SqlTaskClient.PublishAsync(task.Id));
    }

    [Fact(DisplayName = "Publish задания с попытками → ConflictException")]
    public async Task Publish_TaskWithAttempts_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);
        await SeedAttemptAsync(task.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(() => SqlTaskClient.PublishAsync(task.Id));
    }

    [Fact(DisplayName = "Publish задания без проверенного эталона → ValidationException")]
    public async Task Publish_UnvalidatedReferenceQuery_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        Guid taskId;
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = Domain.Training.SqlQuery.Create("SELECT 1", false, false, targetDbId);
            var task = Domain.Training.SqlTask.Create(
                topicId,
                query.Id,
                "Task with unvalidated reference",
                "Text",
                1);
            db.SqlQueries.Add(query);
            db.SqlTasks.Add(task);
            await db.SaveChangesAsync();
            taskId = task.Id;
        }

        // Act + Assert
        await Should.ThrowAsync<ValidationException>(() => SqlTaskClient.PublishAsync(taskId));
    }

    [Fact(DisplayName = "Delete → задание больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var created = await CreateSqlTaskAsync(topicId, sqlQueryId, "ToDelete");

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
                new CreateSqlTaskRequest(Guid.NewGuid(), "Task", "Text", 1, Reference(targetDbId))));
    }

    [Fact(DisplayName = "Create с несуществующей учебной базой → ConflictException")]
    public async Task Create_NonExistentTargetDbId_ThrowsConflictException()
    {
        // Arrange
        var topicId = await CreateTopicAsync();

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(topicId, "Task", "Text", 1, Reference(Guid.NewGuid()))));
    }

    [Fact(DisplayName = "Create с пустым TaskName → ValidationException")]
    public async Task Create_EmptyTaskName_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(Guid.NewGuid(), "", "Text", 1, Reference(Guid.NewGuid()))));

        // Assert
        ex.Problem!.Title.ShouldBe("Ошибка валидации запроса");
        ex.Problem.Code.ShouldBe("Request.ValidationFailed");
        ex.Problem.Detail.ShouldNotBeNullOrWhiteSpace();
        ex.Errors.ShouldContainKey("TaskName");
    }

    [Fact(DisplayName = "Create с DifficultyLevel вне диапазона → ValidationException")]
    public async Task Create_DifficultyLevelOutOfRange_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(Guid.NewGuid(), "Task", "Text", 6, Reference(Guid.NewGuid()))));

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
        var created = await CreateSqlTaskAsync(topicId, sqlQueryId, "WithAttempts");
        await SeedAttemptAsync(created.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.DeleteAsync(created.Id));
    }

    [Fact(DisplayName = "Comparison limit → создание и обновление слишком большого эталона возвращают стабильный 422")]
    public async Task OversizedReference_CreateAndUpdate_ReturnStableValidationError()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId, "Comparison limit");
        var executor = GetFakeExecutor();
        executor.OverrideRun = Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, ["id"], [], 10000, 1, IsTruncated: true));

        try
        {
            var createError = await Should.ThrowAsync<ValidationException>(() =>
                SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
                    topicId, "Too large", "Text", 1, Reference(targetDbId, "SELECT huge"))));
            AssertComparisonLimitError(createError);

            var updateError = await Should.ThrowAsync<ValidationException>(() =>
                SqlTaskClient.UpdateReferenceQueryAsync(task.Id,
                    new UpdateTaskReferenceQueryRequest(targetDbId, "SELECT huge", false, false)));
            AssertComparisonLimitError(updateError);
            executor.LastQuery.ShouldNotBeNull().MaxRows.ShouldBe(10000);
        }
        finally
        {
            executor.OverrideRun = null;
        }
    }

    [Fact(DisplayName = "Comparison limit → старый большой эталон нельзя опубликовать, capability согласована")]
    public async Task OversizedLegacyReference_CannotBePublished()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var task = await CreateSqlTaskAsync(topicId, targetDbId, "Legacy oversized");
        var oversizedRows = Enumerable.Range(1, 10001)
            .Select(value => (IReadOnlyList<string?>)[value.ToString()])
            .ToList();

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = await db.SqlQueries.SingleAsync(x => x.Id == task.SqlQueryId);
            query.SetExpectedResult(GoldenResult.Serialize(
                new QueryResultSet(true, null, ["id"], oversizedRows, oversizedRows.Count, 1)));
            await db.SaveChangesAsync();
        }

        var details = await SqlTaskClient.GetTeacherDetailsAsync(task.Id);
        details.ShouldNotBeNull();
        details.CanPublish.ShouldBeFalse();
        details.LifecycleRestriction.ShouldNotBeNull().ShouldContain("10000");

        var executor = GetFakeExecutor();
        executor.OverrideRun = Result<QueryResultSet>.Success(
            new QueryResultSet(true, null, ["id"], oversizedRows.Take(10000).ToList(), 10000, 1,
                IsTruncated: true));
        try
        {
            var error = await Should.ThrowAsync<ValidationException>(() =>
                SqlTaskClient.PublishAsync(task.Id));
            AssertComparisonLimitError(error);
            executor.LastQuery.ShouldNotBeNull().MaxRows.ShouldBe(10000);
        }
        finally
        {
            executor.OverrideRun = null;
        }
    }

    private static void AssertComparisonLimitError(ValidationException error)
    {
        error.StatusCode.ShouldBe(422);
        error.Problem.ShouldNotBeNull();
        error.Problem.Code.ShouldBe("ReferenceResultExceedsComparisonLimit");
        error.Errors.ShouldContainKey("referenceQuery.queryText");
        var violation = error.Problem.Violations.ShouldHaveSingleItem();
        violation.Path.ShouldBe("referenceQuery.queryText");
        violation.Code.ShouldBe("ReferenceResultExceedsComparisonLimit");
        violation.Limit.ShouldBe(10000);
    }
}
