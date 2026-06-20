using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.TargetDb;

[Collection(IntegrationTestCollection.Name)]
public sealed class TargetDbTests : ApiTestBase
{
    private readonly TestApplication app;

    public TargetDbTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DbmsDictionary.Create(
            "Test_" + Guid.NewGuid(), "test", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);
        await db.SaveChangesAsync();
        return dbms.Id;
    }

    private async Task<TargetDbResponse> CreateTargetDbAsync(Guid dbmsId, string dbName = "TestDB")
        => await TargetDbClient.CreateAsync(new CreateTargetDbRequest(dbmsId, dbName, null, false));

    [Fact(DisplayName = "Create → возвращает TargetDbResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();

        // Act
        var response = await TargetDbClient.CreateAsync(new CreateTargetDbRequest(dbmsId, "HappyDB", "desc", false));

        // Assert
        response.DbName.ShouldBe("HappyDB");
        response.IsReadOnly.ShouldBeFalse();
        response.DbmsId.ShouldBe(dbmsId);
    }

    [Fact(DisplayName = "Create с несуществующим DbmsId → ConflictException (409)")]
    public async Task Create_NonExistentDbmsId_ThrowsConflictException()
    {
        // Act + Assert
        await Should.ThrowAsync<ConflictException>(
            () => TargetDbClient.CreateAsync(new CreateTargetDbRequest(Guid.NewGuid(), "SomeDB", null, false)));
    }

    [Fact(DisplayName = "Create с пустым DbName → ValidationException")]
    public async Task Create_EmptyDbName_ThrowsValidationException()
    {
        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TargetDbClient.CreateAsync(new CreateTargetDbRequest(Guid.NewGuid(), "", null, false)));

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
        // Act
        var result = await TargetDbClient.GetByIdAsync(Guid.NewGuid());

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
        await TargetDbClient.UpdateAsync(created.Id, new UpdateTargetDbRequest("UpdatedDB", null, true));

        // Assert
        var updated = await TargetDbClient.GetByIdAsync(created.Id);
        updated!.DbName.ShouldBe("UpdatedDB");
        updated.IsReadOnly.ShouldBeTrue();
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => TargetDbClient.UpdateAsync(Guid.NewGuid(), new UpdateTargetDbRequest("DB", null, false)));
    }

    [Fact(DisplayName = "Update с пустым DbName → ValidationException")]
    public async Task Update_EmptyDbName_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await CreateTargetDbAsync(dbmsId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TargetDbClient.UpdateAsync(created.Id, new UpdateTargetDbRequest("", null, false)));

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
        // Act + Assert
        await Should.NotThrowAsync(() => TargetDbClient.DeleteAsync(Guid.NewGuid()));
    }
}
