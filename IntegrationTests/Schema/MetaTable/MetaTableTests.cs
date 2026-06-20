using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.MetaTable;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Host.Features.Schema.MetaTables;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.MetaTable;

/// <summary>
/// Интеграционные тесты CRUD-операций для <see cref="IMetaTableClient"/>.
/// Каждый тест независим: создаёт собственные данные через вспомогательные методы.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class MetaTableTests : ApiTestBase
{
    private readonly TestApplication app;

    public MetaTableTests(TestApplication testApplication) : base(testApplication)
    {
        app = testApplication;
    }

    // ── вспомогательные методы ─────────────────────────────────────

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

    private async Task<Guid> CreateTargetDbAsync()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var response = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        return response.Id;
    }

    private async Task<MetaTableResponse> CreateMetaTableAsync(
        Guid targetDbId, string tableName = "orders")
        => await MetaTableClient.CreateAsync(
            new CreateMetaTableRequest(targetDbId, tableName, null));

    /// <summary>
    /// Засевает MetaAttribute напрямую через AppDbContext.
    /// Для PhysicalTypeId создаёт временный PhysicalType внутри метода.
    /// </summary>
    private async Task<Guid> SeedMetaAttributeAsync(Guid metaTableId, Guid dbmsId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var physicalType = PhysicalType.Create(dbmsId, "integer_" + Guid.NewGuid());
        db.PhysicalTypes.Add(physicalType);

        var attr = MetaAttribute.Create(
            metaTableId, physicalType.Id, "col_" + Guid.NewGuid(),
            isPrimaryKey: false, isRequired: false, sortOrder: 1);
        db.MetaAttributes.Add(attr);
        await db.SaveChangesAsync();
        return attr.Id;
    }

    private async Task SeedMetaRelationshipAsync(Guid sourceAttrId, Guid targetAttrId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rel = MetaRelationship.Create(
            "rel_" + Guid.NewGuid(), sourceAttrId, targetAttrId,
            deleteRule: null, updateRule: null);
        db.MetaRelationships.Add(rel);
        await db.SaveChangesAsync();
    }

    // ── happy-path ─────────────────────────────────────────────────

    [Fact(DisplayName = "Create → возвращает MetaTableResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();
        var request = new CreateMetaTableRequest(targetDbId, "users", "Таблица пользователей");

        // Act
        var response = await MetaTableClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.TargetDbId.ShouldBe(targetDbId);
        response.TableName.ShouldBe("users");
        response.Description.ShouldBe("Таблица пользователей");
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную мета-таблицу")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();
        var created = await CreateMetaTableAsync(targetDbId, "products");

        // Act
        var found = await MetaTableClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.TableName.ShouldBe("products");
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную таблицу")]
    public async Task GetAll_ContainsCreatedTable()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();
        var created = await CreateMetaTableAsync(targetDbId);

        // Act
        var page = await MetaTableClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(t => t.Id == created.Id);
    }

    [Fact(DisplayName = "GetAll с фильтром TargetDbId → возвращает только таблицы этой БД")]
    public async Task GetAll_WithTargetDbIdFilter_ReturnsOnlyMatchingTables()
    {
        // Arrange
        var targetDbId1 = await CreateTargetDbAsync();
        var targetDbId2 = await CreateTargetDbAsync();
        var created1 = await CreateMetaTableAsync(targetDbId1, "t1_" + Guid.NewGuid());
        var created2 = await CreateMetaTableAsync(targetDbId2, "t2_" + Guid.NewGuid());

        // Act — фильтруем только по targetDbId1
        var url = $"{SQLModule.Contracts.ApiRoutes.Schema.MetaTables.Collection}?offset=0&limit=100&targetDbId={targetDbId1}";
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PageResponse<MetaTableResponse>>(
            SQLModule.Client.ClientJson.Options);

        // Assert
        page!.Items.ShouldContain(t => t.Id == created1.Id);
        page.Items.ShouldNotContain(t => t.Id == created2.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();
        var created = await CreateMetaTableAsync(targetDbId, "old_name");

        // Act
        await MetaTableClient.UpdateAsync(created.Id,
            new UpdateMetaTableRequest("new_name", "Новое описание"));

        // Assert
        var updated = await MetaTableClient.GetByIdAsync(created.Id);
        updated!.TableName.ShouldBe("new_name");
        updated.Description.ShouldBe("Новое описание");
    }

    [Fact(DisplayName = "Delete → таблица больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();
        var created = await CreateMetaTableAsync(targetDbId);

        // Act
        await MetaTableClient.DeleteAsync(created.Id);

        // Assert
        var found = await MetaTableClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    // ── негатив ────────────────────────────────────────────────────

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var result = await MetaTableClient.GetByIdAsync(Guid.NewGuid());
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        await Should.NotThrowAsync(() => MetaTableClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => MetaTableClient.UpdateAsync(Guid.NewGuid(),
                new UpdateMetaTableRequest("x", null)));
    }

    [Fact(DisplayName = "Create с несуществующим TargetDbId → ConflictException с кодом MetaTableErrors.TargetDbNotFound")]
    public async Task Create_UnknownTargetDbId_ThrowsConflictException()
    {
        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaTableClient.CreateAsync(
                new CreateMetaTableRequest(Guid.NewGuid(), "t", null)));

        // Assert
        ex.Problem!.Title.ShouldBe(MetaTableErrors.TargetDbNotFound(Guid.Empty).Code);
    }

    [Fact(DisplayName = "Create с пустым TableName → ValidationException")]
    public async Task Create_EmptyTableName_ThrowsValidationException()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaTableClient.CreateAsync(
                new CreateMetaTableRequest(targetDbId, "", null)));

        // Assert
        ex.Errors.ShouldContainKey("TableName");
    }

    [Fact(DisplayName = "Update с пустым TableName → ValidationException")]
    public async Task Update_EmptyTableName_ThrowsValidationException()
    {
        // Arrange
        var targetDbId = await CreateTargetDbAsync();
        var created = await CreateMetaTableAsync(targetDbId);

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => MetaTableClient.UpdateAsync(created.Id,
                new UpdateMetaTableRequest("", null)));

        // Assert
        ex.Errors.ShouldContainKey("TableName");
    }

    [Fact(DisplayName = "Delete таблицы с колонками в MetaRelationship → ConflictException с кодом MetaTable.InUse")]
    public async Task Delete_TableWithRelationship_ThrowsConflictException()
    {
        // Arrange — создаём таблицу и две колонки, между которыми есть FK-связь
        var dbmsId = await CreateDbmsDictionaryAsync();
        var targetDb = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "DB_" + Guid.NewGuid(), null, false));
        var table = await CreateMetaTableAsync(targetDb.Id, "tbl_" + Guid.NewGuid());

        var srcAttrId = await SeedMetaAttributeAsync(table.Id, dbmsId);
        var tgtAttrId = await SeedMetaAttributeAsync(table.Id, dbmsId);
        await SeedMetaRelationshipAsync(srcAttrId, tgtAttrId);

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => MetaTableClient.DeleteAsync(table.Id));

        // Assert
        ex.Problem!.Title.ShouldBe(MetaTableErrors.InUse.Code);
    }
}
