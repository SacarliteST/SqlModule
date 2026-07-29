using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.Schema.DataRecords;

namespace SQLModule.IntegrationTests.Schema.DataRecord;

[Collection(IntegrationTestCollection.Name)]
public sealed class DataRecordTests : ApiTestBase
{

    public DataRecordTests(TestApplication testApplication) : base(testApplication)
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

    private async Task<Guid> CreatePrerequisitesAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDbId = await CreateTargetDbAsync(dbmsId);
        return await CreateMetaTableAsync(targetDbId);
    }

    [Fact(DisplayName = "Create → возвращает DataRecordResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();

        // Act
        var response = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, 5));

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.MetaTableId.ShouldBe(metaTableId);
        response.SortOrder.ShouldBe(5);
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную строку данных")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();
        var created = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, null));

        // Act
        var found = await DataRecordClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.MetaTableId.ShouldBe(metaTableId);
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную строку данных")]
    public async Task GetAll_ContainsCreatedRecord()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();
        var created = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, null));

        // Act
        var page = await DataRecordClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(r => r.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром MetaTableId → возвращает только строки этой таблицы")]
    public async Task GetAll_WithMetaTableIdFilter_ReturnsOnlyMatchingRecords()
    {
        // Arrange
        var metaTableId1 = await CreatePrerequisitesAsync();
        var metaTableId2 = await CreatePrerequisitesAsync();
        var rec1 = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId1, null));
        var rec2 = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId2, null));

        // Act
        var url = $"{ApiRoutes.Schema.DataRecords.Collection}?offset=0&limit=100&metaTableId={metaTableId1}";
        var httpResponse = await HttpClient.GetAsync(url);
        httpResponse.EnsureSuccessStatusCode();
        var page = await httpResponse.Content.ReadFromJsonAsync<PageResponse<DataRecordResponse>>(ClientJson.Options);

        // Assert
        page!.Items.ShouldContain(r => r.Id == rec1.Id);
        page.Items.ShouldNotContain(r => r.Id == rec2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();
        var created = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, 1));

        // Act
        await DataRecordClient.UpdateAsync(created.Id, new UpdateDataRecordRequest(99));

        // Assert
        var updated = await DataRecordClient.GetByIdAsync(created.Id);
        updated!.SortOrder.ShouldBe(99);
    }

    [Fact(DisplayName = "Delete → строка данных больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();
        var created = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, null));

        // Act
        await DataRecordClient.DeleteAsync(created.Id);

        // Assert
        var found = await DataRecordClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await DataRecordClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => DataRecordClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => DataRecordClient.UpdateAsync(Guid.NewGuid(), new UpdateDataRecordRequest(1)));
    }

    [Fact(DisplayName = "Create с несуществующим MetaTableId → ConflictException с кодом MetaTableNotFound")]
    public async Task Create_UnknownMetaTableId_ThrowsConflictException()
    {
        // Arrange
        var unknownMetaTableId = Guid.NewGuid();

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => DataRecordClient.CreateAsync(new CreateDataRecordRequest(unknownMetaTableId, null)));

        // Assert
        ex.Problem!.Code.ShouldBe(DataRecordErrors.MetaTableNotFound(unknownMetaTableId).Code);
    }

    [Fact(DisplayName = "Create с отрицательным SortOrder → ValidationException")]
    public async Task Create_NegativeSortOrder_ThrowsValidationException()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, -1)));

        // Assert
        ex.Errors.ShouldContainKey("SortOrder");
    }

    [Fact(DisplayName = "Update с отрицательным SortOrder → ValidationException")]
    public async Task Update_NegativeSortOrder_ThrowsValidationException()
    {
        // Arrange
        var metaTableId = await CreatePrerequisitesAsync();
        var created = await DataRecordClient.CreateAsync(new CreateDataRecordRequest(metaTableId, null));

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => DataRecordClient.UpdateAsync(created.Id, new UpdateDataRecordRequest(-5)));

        // Assert
        ex.Errors.ShouldContainKey("SortOrder");
    }
}
