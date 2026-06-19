using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.TargetDb;

/// <summary>
/// Интеграционные тесты CRUD-операций для <see cref="ITargetDbClient"/>.
/// Каждый тест независим: создаёт собственные данные через вспомогательные методы.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class TargetDbTests : ApiTestBase
{
    public TargetDbTests(TestApplication testApplication) : base(testApplication) { }

    /// <summary>Создаёт запись СУБД-справочника и возвращает её <c>Id</c>.</summary>
    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        var response = await HttpClient.PostAsJsonAsync("api/v1/dbms-dictionaries", new
        {
            DbmsName = "Scanner_" + Guid.NewGuid(),
            DbmsSystemName = "scanner",
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

    /// <summary>Создаёт запись TargetDb для указанной СУБД и возвращает ответ сервера.</summary>
    private async Task<TargetDbResponse> CreateTargetDbAsync(Guid dbmsId, string dbName = "TestDB")
        => await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, dbName, null, false));

    [Fact(DisplayName = "Create → возвращает TargetDbResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var request = new CreateTargetDbRequest(dbmsId, "HappyDB", "desc", false);

        // Act
        var response = await TargetDbClient.CreateAsync(request);

        // Assert
        response.DbName.ShouldBe("HappyDB");
        response.IsReadOnly.ShouldBeFalse();
        response.DbmsId.ShouldBe(dbmsId);
    }

    [Fact(DisplayName = "Create с несуществующим DbmsId → ConflictException (409)")]
    public async Task Create_NonExistentDbmsId_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateTargetDbRequest(Guid.NewGuid(), "SomeDB", null, false);

        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TargetDbClient.CreateAsync(request));
    }

    [Fact(DisplayName = "Create с пустым DbName → ValidationException (с Errors)")]
    public async Task Create_EmptyDbName_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateTargetDbRequest(Guid.NewGuid(), "", null, false);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TargetDbClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldNotBeEmpty();
        ex.Errors.ShouldContainKey("DbName");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную запись")]
    public async Task GetById_ExistingId_ReturnsEntity()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await CreateTargetDbAsync(dbmsId, "GetMe");

        // Act
        var found = await TargetDbClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.DbName.ShouldBe("GetMe");
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var result = await TargetDbClient.GetByIdAsync(unknownId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную запись")]
    public async Task GetAll_ContainsCreatedEntity()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await CreateTargetDbAsync(dbmsId);

        // Act
        var page = await TargetDbClient.GetAllAsync(0, 50);

        // Assert
        page.Items.ShouldContain(x => x.Id == created.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await CreateTargetDbAsync(dbmsId);

        // Act
        await TargetDbClient.UpdateAsync(
            created.Id, new UpdateTargetDbRequest("UpdatedDB", null, true));

        // Assert
        var updated = await TargetDbClient.GetByIdAsync(created.Id);
        updated!.DbName.ShouldBe("UpdatedDB");
        updated.IsReadOnly.ShouldBeTrue();
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        var request = new UpdateTargetDbRequest("DB", null, false);

        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => TargetDbClient.UpdateAsync(unknownId, request));
    }

    [Fact(DisplayName = "Update с пустым DbName → ValidationException (с Errors)")]
    public async Task Update_EmptyDbName_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await CreateTargetDbAsync(dbmsId);
        var request = new UpdateTargetDbRequest("", null, false);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TargetDbClient.UpdateAsync(created.Id, request));

        // Assert
        ex.Errors.ShouldNotBeEmpty();
        ex.Errors.ShouldContainKey("DbName");
    }

    [Fact(DisplayName = "Delete → запись больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemovedFromStore()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await CreateTargetDbAsync(dbmsId);

        // Act
        await TargetDbClient.DeleteAsync(created.Id);

        // Assert
        var afterDelete = await TargetDbClient.GetByIdAsync(created.Id);
        afterDelete.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act + Assert
        await Should.NotThrowAsync(() => TargetDbClient.DeleteAsync(unknownId));
    }
}
