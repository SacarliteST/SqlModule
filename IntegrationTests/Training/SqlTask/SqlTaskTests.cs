using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;
using DomainAttempt = SQLModule.Domain.Training.Attempt;

namespace SQLModule.IntegrationTests.Training.SqlTask;

[Collection(IntegrationTestCollection.Name)]
public sealed class SqlTaskTests : ApiTestBase
{

    public SqlTaskTests(TestApplication testApplication) : base(testApplication)
    {
    }

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
        Guid sqlQueryId,
        string taskName = "Task",
        PublicationStatus publicationStatus = PublicationStatus.Draft)
        => await SqlTaskClient.CreateAsync(
            new CreateSqlTaskRequest(
                topicId,
                sqlQueryId,
                taskName,
                "Текст задания",
                1,
                publicationStatus));

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
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);
        var request = new CreateSqlTaskRequest(topicId, sqlQueryId, "My Task", "Описание", 3);

        // Act
        var response = await SqlTaskClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.TopicId.ShouldBe(topicId);
        response.SqlQueryId.ShouldBe(sqlQueryId);
        response.TaskName.ShouldBe("My Task");
        response.DifficultyLevel.ShouldBe((short)3);
        response.PublicationStatus.ShouldBe(PublicationStatus.Draft);
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

    [Fact(DisplayName = "Update связей Draft без попыток → тема и запрос изменены")]
    public async Task UpdateLinks_DraftWithoutAttempts_PersistsLinks()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var initialTopicId = await CreateTopicAsync();
        var newTopicId = await CreateTopicAsync();
        var initialQueryId = await CreateSqlQueryAsync(targetDbId);
        var newQueryId = await CreateSqlQueryAsync(targetDbId);
        var task = await CreateSqlTaskAsync(initialTopicId, initialQueryId);

        // Act
        await SqlTaskClient.UpdateAsync(
            task.Id,
            new UpdateSqlTaskRequest(
                task.TaskName,
                task.TaskText,
                task.DifficultyLevel,
                TopicId: newTopicId,
                SqlQueryId: newQueryId));

        // Assert
        var updated = await SqlTaskClient.GetByIdAsync(task.Id);
        updated!.TopicId.ShouldBe(newTopicId);
        updated.SqlQueryId.ShouldBe(newQueryId);
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

    [Fact(DisplayName = "Update с несуществующим новым SQL-запросом → ConflictException")]
    public async Task UpdateLinks_UnknownSqlQuery_ThrowsConflictException()
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
                    SqlQueryId: Guid.NewGuid())));
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
                topicId,
                sqlQueryId));

        // Assert
        var updated = await SqlTaskClient.GetByIdAsync(task.Id);
        updated!.TaskName.ShouldBe("Updated published task");
        updated.TopicId.ShouldBe(topicId);
        updated.SqlQueryId.ShouldBe(sqlQueryId);
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
        details.LastAttempts.ShouldHaveSingleItem().StudentName.ShouldBe("Иван Петров");
        details.LastAttempts[0].StudentId.ShouldBe(studentId);
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

    [Fact(DisplayName = "Create с Published → ValidationException")]
    public async Task Create_PublishedStatus_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var topicId = await CreateTopicAsync();
        var sqlQueryId = await CreateSqlQueryAsync(targetDbId);

        // Act + Assert
        await Should.ThrowAsync<ValidationException>(
            () => CreateSqlTaskAsync(topicId, sqlQueryId, publicationStatus: PublicationStatus.Published));
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
        Guid sqlQueryId;
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var query = Domain.Training.SqlQuery.Create("SELECT 1", false, false, targetDbId);
            db.SqlQueries.Add(query);
            await db.SaveChangesAsync();
            sqlQueryId = query.Id;
        }

        var task = await CreateSqlTaskAsync(topicId, sqlQueryId);

        // Act + Assert
        await Should.ThrowAsync<ValidationException>(() => SqlTaskClient.PublishAsync(task.Id));
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
                new CreateSqlTaskRequest(Guid.NewGuid(), sqlQueryId, "Task", "Text", 1)));
    }

    [Fact(DisplayName = "Create с несуществующим SqlQueryId → ConflictException")]
    public async Task Create_NonExistentSqlQueryId_ThrowsConflictException()
    {
        // Arrange
        var topicId = await CreateTopicAsync();

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(topicId, Guid.NewGuid(), "Task", "Text", 1)));
    }

    [Fact(DisplayName = "Create с пустым TaskName → ValidationException")]
    public async Task Create_EmptyTaskName_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => SqlTaskClient.CreateAsync(
                new CreateSqlTaskRequest(Guid.NewGuid(), Guid.NewGuid(), "", "Text", 1)));

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
                new CreateSqlTaskRequest(Guid.NewGuid(), Guid.NewGuid(), "Task", "Text", 6)));

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
}
