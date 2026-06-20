using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.DbmsCatalog.PhysicalType;

[Collection(IntegrationTestCollection.Name)]
public sealed class PhysicalTypeTests : ApiTestBase
{
    private readonly TestApplication app;

    public PhysicalTypeTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    // ── helpers ────────────────────────────────────────────────────

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

    private async Task<Guid> CreatePhysicalTypeAsync(Guid dbmsId, string? typeName = null)
    {
        var response = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, typeName ?? "type_" + Guid.NewGuid().ToString("N")[..8]));
        return response.Id;
    }

    private async Task<Guid> SeedMetaAttributeAsync(Guid physicalTypeId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbms = DbmsDictionary.Create(
            "dbms_" + Guid.NewGuid(), "test", "postgres:latest", 5432,
            "PU", "PP", "PD", null, "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);

        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbms.Id, "db_" + Guid.NewGuid().ToString("N")[..8], null, false));

        var metaTable = await MetaTableClient.CreateAsync(
            new CreateMetaTableRequest(targetDb.Id, "tbl_" + Guid.NewGuid().ToString("N")[..8], null));

        var metaAttr = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTable.Id, physicalTypeId, "col_" + Guid.NewGuid().ToString("N")[..8],
                false, false, 0));

        return metaAttr.Id;
    }

    // ── happy-path ─────────────────────────────────────────────────

    [Fact(DisplayName = "Create → возвращает PhysicalTypeResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var request = new CreatePhysicalTypeRequest(dbmsId, "VARCHAR");

        var response = await PhysicalTypeClient.CreateAsync(request);

        response.Id.ShouldNotBe(Guid.Empty);
        response.DbmsId.ShouldBe(dbmsId);
        response.TypeName.ShouldBe("VARCHAR");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданный физический тип")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "INT"));

        var found = await PhysicalTypeClient.GetByIdAsync(created.Id);

        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.TypeName.ShouldBe("INT");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданный физический тип")]
    public async Task GetAll_ContainsCreatedPhysicalType()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "TEXT_" + Guid.NewGuid().ToString("N")[..6]));

        var page = await PhysicalTypeClient.GetAllAsync(0, 100);

        page.Items.ShouldContain(p => p.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром DbmsId → возвращает только типы этой СУБД")]
    public async Task GetAll_WithDbmsIdFilter_ReturnsOnlyMatchingTypes()
    {
        var dbmsId1 = await CreateDbmsDictionaryAsync();
        var dbmsId2 = await CreateDbmsDictionaryAsync();
        var pt1 = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId1, "INT_" + Guid.NewGuid().ToString("N")[..6]));
        var pt2 = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId2, "BIGINT_" + Guid.NewGuid().ToString("N")[..6]));

        var url = $"{ApiRoutes.DbmsCatalog.PhysicalTypes.Collection}?offset=0&limit=100&dbmsId={dbmsId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<PhysicalTypeResponse>>(
            ClientJson.Options);

        page!.Items.ShouldContain(p => p.Id == pt1.Id);
        page.Items.ShouldNotContain(p => p.Id == pt2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "OLD_TYPE"));

        await PhysicalTypeClient.UpdateAsync(created.Id, new UpdatePhysicalTypeRequest("NEW_TYPE"));

        var updated = await PhysicalTypeClient.GetByIdAsync(created.Id);
        updated!.TypeName.ShouldBe("NEW_TYPE");
    }

    [Fact(DisplayName = "Delete → физический тип больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "DEL_TYPE"));

        await PhysicalTypeClient.DeleteAsync(created.Id);

        var found = await PhysicalTypeClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    // ── негатив ────────────────────────────────────────────────────

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var result = await PhysicalTypeClient.GetByIdAsync(Guid.NewGuid());
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        await Should.NotThrowAsync(() => PhysicalTypeClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => PhysicalTypeClient.UpdateAsync(Guid.NewGuid(), new UpdatePhysicalTypeRequest("X")));
    }

    [Fact(DisplayName = "Create с несуществующим DbmsId → ConflictException с кодом DbmsNotFound")]
    public async Task Create_UnknownDbmsId_ThrowsConflictException()
    {
        var unknownDbmsId = Guid.NewGuid();

        var ex = await Should.ThrowAsync<ConflictException>(
            () => PhysicalTypeClient.CreateAsync(
                new CreatePhysicalTypeRequest(unknownDbmsId, "TYPE")));

        ex.Problem!.Title.ShouldBe(PhysicalTypeErrors.DbmsNotFound(unknownDbmsId).Code);
    }

    [Fact(DisplayName = "Delete типа, используемого MetaAttribute → ConflictException с кодом InUse")]
    public async Task Delete_PhysicalTypeInUseByMetaAttribute_ThrowsConflictException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        await SeedMetaAttributeAsync(physicalTypeId);

        var ex = await Should.ThrowAsync<ConflictException>(
            () => PhysicalTypeClient.DeleteAsync(physicalTypeId));

        ex.Problem!.Title.ShouldBe(PhysicalTypeErrors.InUse.Code);
    }

    [Fact(DisplayName = "Create с пустым TypeName → ValidationException")]
    public async Task Create_EmptyTypeName_ThrowsValidationException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();

        var ex = await Should.ThrowAsync<ValidationException>(
            () => PhysicalTypeClient.CreateAsync(
                new CreatePhysicalTypeRequest(dbmsId, "")));

        ex.Errors.ShouldContainKey("TypeName");
    }

    [Fact(DisplayName = "Update с пустым TypeName → ValidationException")]
    public async Task Update_EmptyTypeName_ThrowsValidationException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "VALID"));

        var ex = await Should.ThrowAsync<ValidationException>(
            () => PhysicalTypeClient.UpdateAsync(created.Id, new UpdatePhysicalTypeRequest("")));

        ex.Errors.ShouldContainKey("TypeName");
    }
}
