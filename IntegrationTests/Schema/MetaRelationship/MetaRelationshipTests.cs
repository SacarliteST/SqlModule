using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.MetaRelationship;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Host.Features.Schema.MetaRelationships;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.MetaRelationship;

[Collection(IntegrationTestCollection.Name)]
public sealed class MetaRelationshipTests : ApiTestBase
{
    private readonly TestApplication app;

    public MetaRelationshipTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    // ── helpers ────────────────────────────────────────────────────

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        var response = await HttpClient.PostAsJsonAsync("api/v1/dbms-dictionaries", new
        {
            DbmsName = "Test_" + Guid.NewGuid(),
            DbmsSystemName = "test",
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

    private async Task<Guid> CreateTargetDbAsync(Guid dbmsId)
    {
        var response = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        return response.Id;
    }

    private async Task<Guid> CreateMetaTableAsync(Guid targetDbId)
    {
        var response = await MetaTableClient.CreateAsync(
            new CreateMetaTableRequest(targetDbId, "tbl_" + Guid.NewGuid().ToString("N")[..8], null));
        return response.Id;
    }

    private async Task<Guid> SeedMetaAttributeAsync(Guid metaTableId, Guid dbmsId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var physicalType = PhysicalType.Create(dbmsId, "int_" + Guid.NewGuid().ToString("N")[..8]);
        db.PhysicalTypes.Add(physicalType);

        var attr = MetaAttribute.Create(
            metaTableId, physicalType.Id, "col_" + Guid.NewGuid().ToString("N")[..8],
            isPrimaryKey: false, isRequired: false, sortOrder: (short)1);
        db.MetaAttributes.Add(attr);
        await db.SaveChangesAsync();
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

    // ── happy-path ─────────────────────────────────────────────────

    [Fact(DisplayName = "Create → возвращает MetaRelationshipResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var request = new CreateMetaRelationshipRequest(
            "fk_test", srcId, tgtId, "CASCADE", "RESTRICT");

        var response = await MetaRelationshipClient.CreateAsync(request);

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
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_get", srcId, tgtId, null, null));

        var found = await MetaRelationshipClient.GetByIdAsync(created.Id);

        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.RelationshipName.ShouldBe("fk_get");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную связь")]
    public async Task GetAll_ContainsCreatedRelationship()
    {
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_all", srcId, tgtId, null, null));

        var page = await MetaRelationshipClient.GetAllAsync(0, 100);

        page.Items.ShouldContain(r => r.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром AttributeId → возвращает только связи с этим атрибутом")]
    public async Task GetAll_WithAttributeIdFilter_ReturnsOnlyMatchingRelationships()
    {
        var (srcId1, tgtId1) = await SeedTwoAttributesAsync();
        var (srcId2, tgtId2) = await SeedTwoAttributesAsync();
        var rel1 = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_filtered_1", srcId1, tgtId1, null, null));
        var rel2 = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_filtered_2", srcId2, tgtId2, null, null));

        var url = $"{ApiRoutes.Schema.MetaRelationships.Collection}?offset=0&limit=100&attributeId={srcId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<MetaRelationshipResponse>>(
            ClientJson.Options);

        page!.Items.ShouldContain(r => r.Id == rel1.Id);
        page.Items.ShouldNotContain(r => r.Id == rel2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_old", srcId, tgtId, null, null));

        await MetaRelationshipClient.UpdateAsync(created.Id,
            new UpdateMetaRelationshipRequest("fk_new", "CASCADE", "NO ACTION"));

        var updated = await MetaRelationshipClient.GetByIdAsync(created.Id);
        updated!.RelationshipName.ShouldBe("fk_new");
        updated.DeleteRule.ShouldBe("CASCADE");
        updated.UpdateRule.ShouldBe("NO ACTION");
    }

    [Fact(DisplayName = "Delete → связь больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_del", srcId, tgtId, null, null));

        await MetaRelationshipClient.DeleteAsync(created.Id);

        var found = await MetaRelationshipClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    // ── негатив ────────────────────────────────────────────────────

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var result = await MetaRelationshipClient.GetByIdAsync(Guid.NewGuid());
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        await Should.NotThrowAsync(() => MetaRelationshipClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => MetaRelationshipClient.UpdateAsync(Guid.NewGuid(),
                new UpdateMetaRelationshipRequest("x", null, null)));
    }

    [Fact(DisplayName = "Create с несуществующим SourceAttributeId → ConflictException с кодом SourceAttributeNotFound")]
    public async Task Create_UnknownSourceAttributeId_ThrowsConflictException()
    {
        var (_, tgtId) = await SeedTwoAttributesAsync();
        var unknownSrcId = Guid.NewGuid();

        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("fk_bad_src", unknownSrcId, tgtId, null, null)));

        ex.Problem!.Title.ShouldBe(MetaRelationshipErrors.SourceAttributeNotFound(unknownSrcId).Code);
    }

    [Fact(DisplayName = "Create с несуществующим TargetAttributeId → ConflictException с кодом TargetAttributeNotFound")]
    public async Task Create_UnknownTargetAttributeId_ThrowsConflictException()
    {
        var (srcId, _) = await SeedTwoAttributesAsync();
        var unknownTgtId = Guid.NewGuid();

        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("fk_bad_tgt", srcId, unknownTgtId, null, null)));

        ex.Problem!.Title.ShouldBe(MetaRelationshipErrors.TargetAttributeNotFound(unknownTgtId).Code);
    }

    [Fact(DisplayName = "Create с Source == Target → ValidationException")]
    public async Task Create_SourceEqualsTarget_ThrowsValidationException()
    {
        var (srcId, _) = await SeedTwoAttributesAsync();

        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("fk_self", srcId, srcId, null, null)));

        ex.Errors.ShouldContainKey("TargetAttributeId");
    }

    [Fact(DisplayName = "Create с пустым RelationshipName → ValidationException")]
    public async Task Create_EmptyRelationshipName_ThrowsValidationException()
    {
        var (srcId, tgtId) = await SeedTwoAttributesAsync();

        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaRelationshipClient.CreateAsync(
                new CreateMetaRelationshipRequest("", srcId, tgtId, null, null)));

        ex.Errors.ShouldContainKey("RelationshipName");
    }

    [Fact(DisplayName = "Update с пустым RelationshipName → ValidationException")]
    public async Task Update_EmptyRelationshipName_ThrowsValidationException()
    {
        var (srcId, tgtId) = await SeedTwoAttributesAsync();
        var created = await MetaRelationshipClient.CreateAsync(
            new CreateMetaRelationshipRequest("fk_upd_val", srcId, tgtId, null, null));

        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaRelationshipClient.UpdateAsync(created.Id,
                new UpdateMetaRelationshipRequest("", null, null)));

        ex.Errors.ShouldContainKey("RelationshipName");
    }
}
