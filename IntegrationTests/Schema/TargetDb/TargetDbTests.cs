using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.Infrastructure;

namespace SQLModule.IntegrationTests.Schema.TargetDb;

[Collection(IntegrationTestCollection.Name)]
public sealed class TargetDbTests : ApiTestBase
{

    public TargetDbTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = App.Services.CreateScope();
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
        response.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.UnixEpoch);
        response.UpdatedAt.ShouldBeGreaterThanOrEqualTo(response.CreatedAt);
        response.CreatedById.ShouldNotBe(Guid.Empty);
        response.UpdatedById.ShouldNotBe(Guid.Empty);
        response.CreatedByName.ShouldBe("Test Teacher");
        response.UpdatedByName.ShouldBe("Test Teacher");
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
        var page = await TargetDbClient.GetAllAsync(0, 100);

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

    [Fact(DisplayName = "Delete базы со связью между колонками → удаляет связь каскадом, без 500")]
    public async Task Delete_TargetDbWithRelationship_Succeeds()
    {
        // Arrange: TargetDb -> MetaTable (x2) -> MetaAttribute (x2) -> MetaRelationship между ними.
        // MetaRelationship -> MetaAttribute настроен как Restrict, а каскад TargetDb -> MetaTable ->
        // MetaAttribute его не подчищает — без явной очистки в DeleteTargetDbHandler это падало
        // необработанным 500 на FK-нарушении.
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await CreateTargetDbAsync(dbmsId, "WithRelationship");
        var tableA = await MetaTableClient.CreateAsync(
            new CreateMetaTableRequest(targetDb.Id, "parent_" + Guid.NewGuid().ToString("N")[..8], null));
        var tableB = await MetaTableClient.CreateAsync(
            new CreateMetaTableRequest(targetDb.Id, "child_" + Guid.NewGuid().ToString("N")[..8], null));
        var physicalType = await AsAdminAsync(() => PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "int_" + Guid.NewGuid().ToString("N")[..8])));
        var columnA = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(tableA.Id, physicalType.Id, "id", true, true, 1));
        var columnB = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(tableB.Id, physicalType.Id, "parent_id", false, true, 1));
        await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_parent", columnB.Id, columnA.Id, null, null));

        // Act + Assert
        await Should.NotThrowAsync(() => TargetDbClient.DeleteAsync(targetDb.Id));
        var afterDelete = await TargetDbClient.GetByIdAsync(targetDb.Id);
        afterDelete.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete базы, на которую ссылается эталонный запрос → 409 TargetDb.InUse")]
    public async Task Delete_TargetDbReferencedBySqlQuery_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await CreateTargetDbAsync(dbmsId, "InUse");
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SqlQueries.Add(SqlQuery.Create("SELECT 1", true, true, targetDb.Id));
            await db.SaveChangesAsync();
        }

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(() => TargetDbClient.DeleteAsync(targetDb.Id));

        // Assert
        ex.Problem?.Code.ShouldBe("TargetDb.InUse");
        var stillThere = await TargetDbClient.GetByIdAsync(targetDb.Id);
        stillThere.ShouldNotBeNull();
    }
}
