using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.Schema.MetaAttributes;

namespace SQLModule.IntegrationTests.Schema.MetaAttribute;

[Collection(IntegrationTestCollection.Name)]
public sealed class MetaAttributeTests : ApiTestBase
{

    public MetaAttributeTests(TestApplication testApplication) : base(testApplication)
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

    private async Task<Guid> CreateTargetDbAsync(Guid dbmsId)
        => (await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false))).Id;

    private async Task<Guid> CreateMetaTableAsync(Guid targetDbId)
        => (await MetaTableClient.CreateAsync(
            new CreateMetaTableRequest(targetDbId, "tbl_" + Guid.NewGuid().ToString("N")[..8], null))).Id;

    private async Task<(Guid metaTableId, Guid physicalTypeId)> CreatePrerequisitesAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var metaTableId = await CreateMetaTableAsync(targetDbId);
        var pt = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "type_" + Guid.NewGuid().ToString("N")[..8]));
        return (metaTableId, pt.Id);
    }

    private async Task<Guid> SeedMetaAttributeAsync(Guid metaTableId, Guid physicalTypeId)
        => (await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_" + Guid.NewGuid().ToString("N")[..8], false, false, 1))).Id;

    private async Task SeedMetaRelationshipAsync(Guid sourceAttrId, Guid targetAttrId)
    {
        await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_" + Guid.NewGuid().ToString("N")[..8], sourceAttrId, targetAttrId, null, null));
    }

    [Fact(DisplayName = "Create → возвращает MetaAttributeResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();

        // Act
        var response = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_name", false, true, 1));

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.MetaTableId.ShouldBe(metaTableId);
        response.PhysicalTypeId.ShouldBe(physicalTypeId);
        response.AttributeName.ShouldBe("col_name");
        response.IsPrimaryKey.ShouldBe(false);
        response.IsRequired.ShouldBe(true);
        response.SortOrder.ShouldBe((short)1);
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданный мета-атрибут")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();
        var created = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_get", false, false, 0));

        // Act
        var found = await MetaAttributeClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.AttributeName.ShouldBe("col_get");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданный мета-атрибут")]
    public async Task GetAll_ContainsCreatedAttribute()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();
        var created = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_all", false, false, 0));

        // Act
        var page = await MetaAttributeClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(a => a.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром MetaTableId → возвращает только атрибуты этой таблицы")]
    public async Task GetAll_WithMetaTableIdFilter_ReturnsOnlyMatchingAttributes()
    {
        // Arrange
        var (metaTableId1, physicalTypeId1) = await CreatePrerequisitesAsync();
        var (metaTableId2, physicalTypeId2) = await CreatePrerequisitesAsync();
        var attr1 = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId1, physicalTypeId1, "col_f1", false, false, 0));
        var attr2 = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId2, physicalTypeId2, "col_f2", false, false, 0));

        // Act
        var url = $"{ApiRoutes.Schema.MetaAttributes.Collection}?offset=0&limit=100&metaTableId={metaTableId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<MetaAttributeResponse>>(ClientJson.Options);

        // Assert
        page!.Items.ShouldContain(a => a.Id == attr1.Id);
        page.Items.ShouldNotContain(a => a.Id == attr2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();
        var created = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_old", false, false, 1));

        // Act
        await MetaAttributeClient.UpdateAsync(created.Id,
            new UpdateMetaAttributeRequest("col_new", true, true, 5));

        // Assert
        var updated = await MetaAttributeClient.GetByIdAsync(created.Id);
        updated!.AttributeName.ShouldBe("col_new");
        updated.IsPrimaryKey.ShouldBe(true);
        updated.IsRequired.ShouldBe(true);
        updated.SortOrder.ShouldBe((short)5);
    }

    [Fact(DisplayName = "Delete → атрибут больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();
        var created = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_del", false, false, 0));

        // Act
        await MetaAttributeClient.DeleteAsync(created.Id);

        // Assert
        var found = await MetaAttributeClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await MetaAttributeClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => MetaAttributeClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => MetaAttributeClient.UpdateAsync(Guid.NewGuid(),
                new UpdateMetaAttributeRequest("col_x", false, false, 0)));
    }

    [Fact(DisplayName = "Create с несуществующим MetaTableId → ConflictException с кодом MetaTableNotFound")]
    public async Task Create_UnknownMetaTableId_ThrowsConflictException()
    {
        // Arrange
        var (_, physicalTypeId) = await CreatePrerequisitesAsync();
        var unknownMetaTableId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaAttributeClient.CreateAsync(
                new CreateMetaAttributeRequest(unknownMetaTableId, physicalTypeId, "col_bad_tbl", false, false, 0)));

        // Assert
        ex.Problem!.Title.ShouldBe(MetaAttributeErrors.MetaTableNotFound(unknownMetaTableId).Code);
    }

    [Fact(DisplayName = "Create с несуществующим PhysicalTypeId → ConflictException с кодом PhysicalTypeNotFound")]
    public async Task Create_UnknownPhysicalTypeId_ThrowsConflictException()
    {
        // Arrange
        var (metaTableId, _) = await CreatePrerequisitesAsync();
        var unknownPhysicalTypeId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaAttributeClient.CreateAsync(
                new CreateMetaAttributeRequest(metaTableId, unknownPhysicalTypeId, "col_bad_type", false, false, 0)));

        // Assert
        ex.Problem!.Title.ShouldBe(MetaAttributeErrors.PhysicalTypeNotFound(unknownPhysicalTypeId).Code);
    }

    [Fact(DisplayName = "Create с пустым AttributeName → ValidationException")]
    public async Task Create_EmptyAttributeName_ThrowsValidationException()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaAttributeClient.CreateAsync(
                new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "", false, false, 0)));

        // Assert
        ex.Errors.ShouldContainKey("AttributeName");
    }

    [Fact(DisplayName = "Update с пустым AttributeName → ValidationException")]
    public async Task Update_EmptyAttributeName_ThrowsValidationException()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();
        var created = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, physicalTypeId, "col_upd_val", false, false, 0));

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaAttributeClient.UpdateAsync(created.Id,
                new UpdateMetaAttributeRequest("", false, false, 0)));

        // Assert
        ex.Errors.ShouldContainKey("AttributeName");
    }

    [Fact(DisplayName = "Delete колонки, участвующей в FK-связи → ConflictException с кодом InUse")]
    public async Task Delete_AttributeUsedInRelationship_ThrowsConflictException()
    {
        // Arrange
        var (metaTableId, physicalTypeId) = await CreatePrerequisitesAsync();
        var srcId = await SeedMetaAttributeAsync(metaTableId, physicalTypeId);
        var tgtId = await SeedMetaAttributeAsync(metaTableId, physicalTypeId);
        await SeedMetaRelationshipAsync(srcId, tgtId);

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaAttributeClient.DeleteAsync(srcId));

        // Assert
        ex.Problem!.Title.ShouldBe(MetaAttributeErrors.InUse.Code);
    }
}
