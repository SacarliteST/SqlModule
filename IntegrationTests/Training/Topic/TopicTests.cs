using Shouldly;
using SQLModule.Client;
using SQLModule.Client.Topic;
using SQLModule.Contracts.Training.Topic;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.Topic;

/// <summary>
/// Интеграционные тесты CRUD-операций и перемещения тем через <see cref="ITopicClient"/>.
/// Каждый тест независим: создаёт собственные данные через вспомогательные методы.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TopicTests : ApiTestBase
{
    public TopicTests(TestApplication testApplication) : base(testApplication) { }

    // ─── helpers ─────────────────────────────────────────────────────────────

    /// <summary>Создаёт тему и возвращает ответ сервера.</summary>
    private async Task<TopicResponse> CreateTopicAsync(string topicName, Guid? parentTopicId = null)
        => await TopicClient.CreateAsync(new CreateTopicRequest(topicName, parentTopicId));

    // ─── create ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create (корневая) → возвращает TopicResponse с корректными полями")]
    public async Task Create_RootTopic_ReturnsResponse()
    {
        // Arrange
        var request = new CreateTopicRequest("Root Topic", null);

        // Act
        var response = await TopicClient.CreateAsync(request);

        // Assert
        response.TopicName.ShouldBe("Root Topic");
        response.ParentTopicId.ShouldBeNull();
        response.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact(DisplayName = "Create (с родителем) → ParentTopicId установлен")]
    public async Task Create_WithParent_SetsParentTopicId()
    {
        // Arrange
        var parent = await CreateTopicAsync("Parent_" + Guid.NewGuid());
        var request = new CreateTopicRequest("Child", parent.Id);

        // Act
        var response = await TopicClient.CreateAsync(request);

        // Assert
        response.ParentTopicId.ShouldBe(parent.Id);
    }

    [Fact(DisplayName = "Create с несуществующим ParentTopicId → ConflictException")]
    public async Task Create_NonExistentParentTopicId_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateTopicRequest("Topic", Guid.NewGuid());

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TopicClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с пустым TopicName → ValidationException")]
    public async Task Create_EmptyTopicName_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTopicRequest("", null);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TopicClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("TopicName");
    }

    // ─── get by id ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetById → возвращает ранее созданную тему")]
    public async Task GetById_ExistingId_ReturnsTopic()
    {
        // Arrange
        var created = await CreateTopicAsync("GetMe_" + Guid.NewGuid());

        // Act
        var found = await TopicClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.TopicName.ShouldBe(created.TopicName);
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var result = await TopicClient.GetByIdAsync(unknownId);

        // Assert
        result.ShouldBeNull();
    }

    // ─── get all ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetAll → страница содержит созданную тему")]
    public async Task GetAll_ContainsCreatedTopic()
    {
        // Arrange
        var created = await CreateTopicAsync("Paged_" + Guid.NewGuid());

        // Act
        var page = await TopicClient.GetAllAsync(0, 50);

        // Assert
        page.Items.ShouldContain(t => t.Id == created.Id);
    }

    // ─── update ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var created = await CreateTopicAsync("OldName_" + Guid.NewGuid());

        // Act
        await TopicClient.UpdateAsync(created.Id, new UpdateTopicRequest("NewName"));

        // Assert
        var updated = await TopicClient.GetByIdAsync(created.Id);
        updated!.TopicName.ShouldBe("NewName");
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        var request = new UpdateTopicRequest("Name");

        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => TopicClient.UpdateAsync(unknownId, request));
    }

    [Fact(DisplayName = "Update с пустым TopicName → ValidationException")]
    public async Task Update_EmptyTopicName_ThrowsValidationException()
    {
        // Arrange
        var created = await CreateTopicAsync("SomeName_" + Guid.NewGuid());
        var request = new UpdateTopicRequest("");

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TopicClient.UpdateAsync(created.Id, request));

        // Assert
        ex.Errors.ShouldContainKey("TopicName");
    }

    // ─── delete ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Delete → тема больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var created = await CreateTopicAsync("ToDelete_" + Guid.NewGuid());

        // Act
        await TopicClient.DeleteAsync(created.Id);

        // Assert
        var found = await TopicClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act + Assert
        await Should.NotThrowAsync(() => TopicClient.DeleteAsync(unknownId));
    }

    [Fact(DisplayName = "Delete темы с подтемами → ConflictException")]
    public async Task Delete_TopicWithSubTopics_ThrowsConflictException()
    {
        // Arrange
        var parent = await CreateTopicAsync("Parent_" + Guid.NewGuid());
        _ = await CreateTopicAsync("Child", parent.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TopicClient.DeleteAsync(parent.Id));
    }

    // ─── move ────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Move → ParentTopicId изменён в БД")]
    public async Task Move_ExistingTopic_ChangesParent()
    {
        // Arrange
        var topicA = await CreateTopicAsync("TopicA_" + Guid.NewGuid());
        var topicB = await CreateTopicAsync("TopicB_" + Guid.NewGuid());

        // Act
        await TopicClient.MoveAsync(topicA.Id, new MoveTopicRequest(topicB.Id));

        // Assert
        var moved = await TopicClient.GetByIdAsync(topicA.Id);
        moved!.ParentTopicId.ShouldBe(topicB.Id);
    }

    [Fact(DisplayName = "Move → сделать корневой (ParentTopicId = null)")]
    public async Task Move_ToRoot_ClearsParent()
    {
        // Arrange
        var parent = await CreateTopicAsync("Parent_" + Guid.NewGuid());
        var child = await CreateTopicAsync("Child", parent.Id);

        // Act
        await TopicClient.MoveAsync(child.Id, new MoveTopicRequest(null));

        // Assert
        var moved = await TopicClient.GetByIdAsync(child.Id);
        moved!.ParentTopicId.ShouldBeNull();
    }

    [Fact(DisplayName = "Move несуществующей темы → NotFoundException")]
    public async Task Move_UnknownId_ThrowsNotFoundException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => TopicClient.MoveAsync(unknownId, new MoveTopicRequest(null)));
    }

    [Fact(DisplayName = "Move под несуществующего родителя → ConflictException")]
    public async Task Move_NonExistentParent_ThrowsConflictException()
    {
        // Arrange
        var topic = await CreateTopicAsync("Topic_" + Guid.NewGuid());

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TopicClient.MoveAsync(topic.Id, new MoveTopicRequest(Guid.NewGuid())));
    }

    [Fact(DisplayName = "Move под себя → ConflictException (цикл)")]
    public async Task Move_UnderSelf_ThrowsConflictException()
    {
        // Arrange
        var topic = await CreateTopicAsync("Topic_" + Guid.NewGuid());

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TopicClient.MoveAsync(topic.Id, new MoveTopicRequest(topic.Id)));
    }

    [Fact(DisplayName = "Move под потомка → ConflictException (цикл)")]
    public async Task Move_UnderDescendant_ThrowsConflictException()
    {
        // Arrange — A → B → C, перемещаем A под C
        var topicA = await CreateTopicAsync("A_" + Guid.NewGuid());
        var topicB = await CreateTopicAsync("B", topicA.Id);
        var topicC = await CreateTopicAsync("C", topicB.Id);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TopicClient.MoveAsync(topicA.Id, new MoveTopicRequest(topicC.Id)));
    }
}
