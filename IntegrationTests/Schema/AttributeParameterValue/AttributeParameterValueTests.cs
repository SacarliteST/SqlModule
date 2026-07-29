using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.Schema.AttributeParameterValues;

namespace SQLModule.IntegrationTests.Schema.AttributeParameterValue;

[Collection(IntegrationTestCollection.Name)]
public sealed class AttributeParameterValueTests : ApiTestBase
{

    public AttributeParameterValueTests(TestApplication testApplication) : base(testApplication)
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

    private async Task<(Guid metaAttributeId, Guid parameterDefinitionId)> CreatePrerequisitesAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        var metaTableId = await CreateMetaTableAsync(targetDbId);

        var pt = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbmsId, "type_" + Guid.NewGuid().ToString("N")[..8]));

        var paramDef = await ParameterDefinitionClient.CreateAsync(
            new CreateParameterDefinitionRequest(pt.Id,
                "key_" + Guid.NewGuid().ToString("N")[..8], "Display", "text",
                null, 1, "{value}", false, null, null, null));

        var attr = await MetaAttributeClient.CreateAsync(
            new CreateMetaAttributeRequest(metaTableId, pt.Id,
                "col_" + Guid.NewGuid().ToString("N")[..8], false, false, 1));

        return (attr.Id, paramDef.Id);
    }

    [Fact(DisplayName = "Create → возвращает AttributeParameterValueResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();

        // Act
        var response = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "val_123"));

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.MetaAttributeId.ShouldBe(attrId);
        response.ParameterDefinitionId.ShouldBe(paramDefId);
        response.ParameterValue.ShouldBe("val_123");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданное значение параметра")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();
        var created = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "get_val"));

        // Act
        var found = await AttributeParameterValueClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.ParameterValue.ShouldBe("get_val");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданное значение параметра")]
    public async Task GetAll_ContainsCreatedValue()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();
        var created = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "all_val"));

        // Act
        var page = await AttributeParameterValueClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(v => v.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром MetaAttributeId → возвращает только значения этой колонки")]
    public async Task GetAll_WithMetaAttributeIdFilter_ReturnsOnlyMatchingValues()
    {
        // Arrange
        var (attrId1, paramDefId1) = await CreatePrerequisitesAsync();
        var (attrId2, paramDefId2) = await CreatePrerequisitesAsync();
        var val1 = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId1, paramDefId1, "filtered_1"));
        var val2 = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId2, paramDefId2, "filtered_2"));

        // Act
        var url = $"{ApiRoutes.Schema.AttributeParameterValues.Collection}?offset=0&limit=100&metaAttributeId={attrId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<AttributeParameterValueResponse>>(ClientJson.Options);

        // Assert
        page!.Items.ShouldContain(v => v.Id == val1.Id);
        page.Items.ShouldNotContain(v => v.Id == val2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();
        var created = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "old_val"));

        // Act
        await AttributeParameterValueClient.UpdateAsync(created.Id,
            new UpdateAttributeParameterValueRequest("new_val"));

        // Assert
        var updated = await AttributeParameterValueClient.GetByIdAsync(created.Id);
        updated!.ParameterValue.ShouldBe("new_val");
    }

    [Fact(DisplayName = "Delete → значение параметра больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();
        var created = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "del_val"));

        // Act
        await AttributeParameterValueClient.DeleteAsync(created.Id);

        // Assert
        var found = await AttributeParameterValueClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await AttributeParameterValueClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => AttributeParameterValueClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => AttributeParameterValueClient.UpdateAsync(Guid.NewGuid(),
                new UpdateAttributeParameterValueRequest("x")));
    }

    [Fact(DisplayName = "Create с несуществующим MetaAttributeId → ConflictException с кодом MetaAttributeNotFound")]
    public async Task Create_UnknownMetaAttributeId_ThrowsConflictException()
    {
        // Arrange
        var (_, paramDefId) = await CreatePrerequisitesAsync();
        var unknownAttrId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => AttributeParameterValueClient.CreateAsync(
                new CreateAttributeParameterValueRequest(unknownAttrId, paramDefId, "val")));

        // Assert
        ex.Problem!.Code.ShouldBe(AttributeParameterValueErrors.MetaAttributeNotFound(unknownAttrId).Code);
    }

    [Fact(DisplayName = "Create с несуществующим ParameterDefinitionId → ConflictException с кодом ParameterDefinitionNotFound")]
    public async Task Create_UnknownParameterDefinitionId_ThrowsConflictException()
    {
        // Arrange
        var (attrId, _) = await CreatePrerequisitesAsync();
        var unknownParamDefId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => AttributeParameterValueClient.CreateAsync(
                new CreateAttributeParameterValueRequest(attrId, unknownParamDefId, "val")));

        // Assert
        ex.Problem!.Code.ShouldBe(AttributeParameterValueErrors.ParameterDefinitionNotFound(unknownParamDefId).Code);
    }

    [Fact(DisplayName = "Повторный Create той же пары → ConflictException с кодом AlreadyExists")]
    public async Task Create_DuplicatePair_ThrowsConflictException()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();
        await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "first_val"));

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => AttributeParameterValueClient.CreateAsync(
                new CreateAttributeParameterValueRequest(attrId, paramDefId, "second_val")));

        // Assert
        ex.Problem!.Code.ShouldBe(AttributeParameterValueErrors.AlreadyExists.Code);
    }

    [Fact(DisplayName = "Create с пустым ParameterValue → ValidationException")]
    public async Task Create_EmptyParameterValue_ThrowsValidationException()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => AttributeParameterValueClient.CreateAsync(
                new CreateAttributeParameterValueRequest(attrId, paramDefId, "")));

        // Assert
        ex.Errors.ShouldContainKey("ParameterValue");
    }

    [Fact(DisplayName = "Update с пустым ParameterValue → ValidationException")]
    public async Task Update_EmptyParameterValue_ThrowsValidationException()
    {
        // Arrange
        var (attrId, paramDefId) = await CreatePrerequisitesAsync();
        var created = await AttributeParameterValueClient.CreateAsync(
            new CreateAttributeParameterValueRequest(attrId, paramDefId, "upd_val"));

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => AttributeParameterValueClient.UpdateAsync(created.Id,
                new UpdateAttributeParameterValueRequest("")));

        // Assert
        ex.Errors.ShouldContainKey("ParameterValue");
    }
}
