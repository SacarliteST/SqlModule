using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Data.Core;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;
using DomainDbmsDictionary = SQLModule.Domain.DbmsCatalog.DbmsDictionary;

namespace SQLModule.IntegrationTests.DbmsCatalog.ParameterDefinition;

[Collection(IntegrationTestCollection.Name)]
public sealed class ParameterDefinitionTests : ApiTestBase
{
    private readonly TestApplication app;

    public ParameterDefinitionTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DomainDbmsDictionary.Create(
            "Test_" + Guid.NewGuid(), "test", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);
        await db.SaveChangesAsync();
        return dbms.Id;
    }

    private async Task<Guid> CreatePhysicalTypeAsync(Guid dbmsId)
    {
        var response = await PhysicalTypeClient.CreateAsync(
            new Contracts.DbmsCatalog.PhysicalType.CreatePhysicalTypeRequest(
                dbmsId, "type_" + Guid.NewGuid().ToString("N")[..8]));
        return response.Id;
    }

    private CreateParameterDefinitionRequest BuildRequest(Guid physicalTypeId, string? key = null) =>
        new(physicalTypeId,
            key ?? "key_" + Guid.NewGuid().ToString("N")[..8],
            "Display Name",
            "text",
            null,
            0,
            "{value}",
            false,
            null,
            null,
            null);

    [Fact(DisplayName = "Create → возвращает ParameterDefinitionResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var request = BuildRequest(physicalTypeId, "param_key");

        // Act
        var response = await ParameterDefinitionClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.PhysicalTypeId.ShouldBe(physicalTypeId);
        response.ParameterKey.ShouldBe("param_key");
        response.DisplayName.ShouldBe("Display Name");
        response.InputType.ShouldBe("text");
        response.SortOrder.ShouldBe((short)0);
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданное определение параметра")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var created = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId, "get_key"));

        // Act
        var found = await ParameterDefinitionClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.ParameterKey.ShouldBe("get_key");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданное определение параметра")]
    public async Task GetAll_ContainsCreatedParameterDefinition()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var created = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId));

        // Act
        var page = await ParameterDefinitionClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(p => p.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром PhysicalTypeId → возвращает только параметры этого типа")]
    public async Task GetAll_WithPhysicalTypeIdFilter_ReturnsOnlyMatchingDefinitions()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId1 = await CreatePhysicalTypeAsync(dbmsId);
        var physicalTypeId2 = await CreatePhysicalTypeAsync(dbmsId);
        var pd1 = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId1));
        var pd2 = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId2));

        // Act
        var url = $"{ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection}?offset=0&limit=100&physicalTypeId={physicalTypeId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<ParameterDefinitionResponse>>(ClientJson.Options);

        // Assert
        page!.Items.ShouldContain(p => p.Id == pd1.Id);
        page.Items.ShouldNotContain(p => p.Id == pd2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var created = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId, "old_key"));

        // Act
        await ParameterDefinitionClient.UpdateAsync(created.Id,
            new UpdateParameterDefinitionRequest(
                "new_key", "New Display", "number", "100", 1, "{value}", true, "(", ")", ","));

        // Assert
        var updated = await ParameterDefinitionClient.GetByIdAsync(created.Id);
        updated!.ParameterKey.ShouldBe("new_key");
        updated.DisplayName.ShouldBe("New Display");
        updated.InputType.ShouldBe("number");
        updated.SortOrder.ShouldBe((short)1);
    }

    [Fact(DisplayName = "Delete → определение параметра больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var created = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId));

        // Act
        await ParameterDefinitionClient.DeleteAsync(created.Id);

        // Assert
        var found = await ParameterDefinitionClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await ParameterDefinitionClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => ParameterDefinitionClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => ParameterDefinitionClient.UpdateAsync(Guid.NewGuid(),
                new UpdateParameterDefinitionRequest("k", "d", "text", null, 0, "{value}", false, null, null, null)));
    }

    [Fact(DisplayName = "Create с несуществующим PhysicalTypeId → ConflictException с кодом PhysicalTypeNotFound")]
    public async Task Create_UnknownPhysicalTypeId_ThrowsConflictException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => ParameterDefinitionClient.CreateAsync(BuildRequest(unknownId)));

        // Assert
        ex.Problem!.Title.ShouldBe(ParameterDefinitionErrors.PhysicalTypeNotFound(unknownId).Code);
    }

    [Fact(DisplayName = "Повторный Create того же ParameterKey для типа → ConflictException с кодом AlreadyExists")]
    public async Task Create_DuplicateParameterKey_ThrowsConflictException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var key = "dup_key_" + Guid.NewGuid().ToString("N")[..6];
        await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId, key));

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId, key)));

        // Assert
        ex.Problem!.Title.ShouldBe(ParameterDefinitionErrors.AlreadyExists.Code);
    }

    [Fact(DisplayName = "Create с пустым ParameterKey → ValidationException")]
    public async Task Create_EmptyParameterKey_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => ParameterDefinitionClient.CreateAsync(
                new CreateParameterDefinitionRequest(physicalTypeId, "", "Display", "text", null, 0, "{value}", false, null, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("ParameterKey");
    }

    [Fact(DisplayName = "Create с пустым DisplayName → ValidationException")]
    public async Task Create_EmptyDisplayName_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => ParameterDefinitionClient.CreateAsync(
                new CreateParameterDefinitionRequest(physicalTypeId, "key", "", "text", null, 0, "{value}", false, null, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("DisplayName");
    }

    [Fact(DisplayName = "Create с пустым InputType → ValidationException")]
    public async Task Create_EmptyInputType_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => ParameterDefinitionClient.CreateAsync(
                new CreateParameterDefinitionRequest(physicalTypeId, "key", "Display", "", null, 0, "{value}", false, null, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("InputType");
    }

    [Fact(DisplayName = "Create с пустым SqlFragment → ValidationException")]
    public async Task Create_EmptySqlFragment_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => ParameterDefinitionClient.CreateAsync(
                new CreateParameterDefinitionRequest(physicalTypeId, "key", "Display", "text", null, 0, "", false, null, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("SqlFragment");
    }

    [Fact(DisplayName = "Update с пустым ParameterKey → ValidationException")]
    public async Task Update_EmptyParameterKey_ThrowsValidationException()
    {
        // Arrange
        var dbmsId = await CreateDbmsDictionaryAsync();
        var physicalTypeId = await CreatePhysicalTypeAsync(dbmsId);
        var created = await ParameterDefinitionClient.CreateAsync(BuildRequest(physicalTypeId));

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => ParameterDefinitionClient.UpdateAsync(created.Id,
                new UpdateParameterDefinitionRequest("", "Display", "text", null, 0, "{value}", false, null, null, null)));

        // Assert
        ex.Errors.ShouldContainKey("ParameterKey");
    }
}
