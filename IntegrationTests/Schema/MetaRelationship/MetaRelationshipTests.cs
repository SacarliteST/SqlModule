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
using SQLModule.Web.Features.Schema.MetaRelationships;

namespace SQLModule.IntegrationTests.Schema.MetaRelationship;

[Collection(IntegrationTestCollection.Name)]
public sealed class MetaRelationshipTests : ApiTestBase
{

    public MetaRelationshipTests(TestApplication testApplication) : base(testApplication)
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

    private async Task<Guid> SeedMetaAttributeAsync(Guid metaTableId, Guid dbmsId)
    {
        var pt = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "int_" + Guid.NewGuid().ToString("N")[..8]));
        var attr = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, pt.Id, "col_" + Guid.NewGuid().ToString("N")[..8], false, false, 1));
        return attr.Id;
    }

    private async Task<(Guid srcId, Guid tgtId)> SeedTwoAttributesAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var tableId = await CreateMetaTableAsync(targetDbId);
        var srcId = await SeedMetaAttributeAsync(tableId, dbmsId);
        var tgtId = await SeedMetaAttributeAsync(tableId, dbmsId);
        return (srcId, tgtId);
    }

    [Fact(DisplayName = "Create → возвращает MetaRelationshipResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();

        // Act
        var response = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_test", srcId, tgtId, "CASCADE", "RESTRICT"));

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.RelationshipName.ShouldBe("fk_test");
        response.SourceAttributeId.ShouldBe(srcId);
        response.TargetAttributeId.ShouldBe(tgtId);
        response.DeleteRule.ShouldBe("CASCADE");
        response.UpdateRule.ShouldBe("RESTRICT");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную связь")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_get", srcId, tgtId, null, null));

        // Act
        var found = await MetaRelationshipClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.RelationshipName.ShouldBe("fk_get");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную связь")]
    public async Task GetAll_ContainsCreatedRelationship()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_all", srcId, tgtId, null, null));

        // Act
        var page = await MetaRelationshipClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(r => r.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром AttributeId → возвращает только связи с этим атрибутом")]
    public async Task GetAll_WithAttributeIdFilter_ReturnsOnlyMatchingRelationships()
    {
        // Arrange
        var (srcId1, tgtId1) = await SeedTwoAttributesAsync();
        var (srcId2, tgtId2) = await SeedTwoAttributesAsync();
        var rel1 = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_filtered_1", srcId1, tgtId1, null, null));
        var rel2 = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_filtered_2", srcId2, tgtId2, null, null));

        // Act
        var url = $"{ApiRoutes.Schema.MetaRelationships.Collection}?offset=0&limit=100&attributeId={srcId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<MetaRelationshipResponse>>(ClientJson.Options);

        // Assert
        page!.Items.ShouldContain(r => r.Id == rel1.Id);
        page.Items.ShouldNotContain(r => r.Id == rel2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_old", srcId, tgtId, null, null));

        // Act
        await MetaRelationshipClient.UpdateAsync(created.Id,
            new UpdateMetaRelationshipRequest("fk_new", "CASCADE", "NO ACTION"));

        // Assert
        var updated = await MetaRelationshipClient.GetByIdAsync(created.Id);
        updated!.RelationshipName.ShouldBe("fk_new");
        updated.DeleteRule.ShouldBe("CASCADE");
        updated.UpdateRule.ShouldBe("NO ACTION");
    }

    [Fact(DisplayName = "Delete → связь больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_del", srcId, tgtId, null, null));

        // Act
        await MetaRelationshipClient.DeleteAsync(created.Id);

        // Assert
        var found = await MetaRelationshipClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await MetaRelationshipClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => MetaRelationshipClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => MetaRelationshipClient.UpdateAsync(Guid.NewGuid(),
                new UpdateMetaRelationshipRequest("x", null, null)));
    }

    [Fact(DisplayName = "Create с несуществующим SourceAttributeId → ConflictException с кодом SourceAttributeNotFound")]
    public async Task Create_UnknownSourceAttributeId_ThrowsConflictException()
    {
        // Arrange
        var (_, tgtId) = await SeedTwoAttributesAsync();
        var unknownSrcId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("fk_bad_src", unknownSrcId, tgtId, null, null)));

        // Assert
        ex.Problem!.Code.ShouldBe(MetaRelationshipErrors.SourceAttributeNotFound(unknownSrcId).Code);
    }

    [Fact(DisplayName = "Create с несуществующим TargetAttributeId → ConflictException с кодом TargetAttributeNotFound")]
    public async Task Create_UnknownTargetAttributeId_ThrowsConflictException()
    {
        // Arrange
        var (srcId, _) = await SeedTwoAttributesAsync();
        var unknownTgtId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("fk_bad_tgt", srcId, unknownTgtId, null, null)));

        // Assert
        ex.Problem!.Code.ShouldBe(MetaRelationshipErrors.TargetAttributeNotFound(unknownTgtId).Code);
    }

    [Fact(DisplayName = "Create с Source == Target → ValidationException")]
    public async Task Create_SourceEqualsTarget_ThrowsValidationException()
    {
        // Arrange
        var (srcId, _) = await SeedTwoAttributesAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("fk_self", srcId, srcId, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("TargetAttributeId");
    }

    [Fact(DisplayName = "Create с пустым RelationshipName → ValidationException")]
    public async Task Create_EmptyRelationshipName_ThrowsValidationException()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("", srcId, tgtId, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("RelationshipName");
    }

    [Fact(DisplayName = "Update с пустым RelationshipName → ValidationException")]
    public async Task Update_EmptyRelationshipName_ThrowsValidationException()
    {
        // Arrange
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_upd_val", srcId, tgtId, null, null));

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaRelationshipClient.UpdateAsync(created.Id,
                new UpdateMetaRelationshipRequest("", null, null)));

        // Assert
        ex.Errors.ShouldContainKey("RelationshipName");
    }
}
