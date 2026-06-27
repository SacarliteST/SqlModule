using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.SchemaBuilder;

[Collection(IntegrationTestCollection.Name)]
public sealed class CreateSchemaTests : ApiTestBase
{

    public CreateSchemaTests(TestApplication testApplication) : base(testApplication)
    {
    }

    // ── E1: успешное создание + tempId resolution ────────────────────────────

    [Fact(DisplayName = "E1: валидная схема → 201, корректная карта tempId→Id")]
    public async Task CreateAsync_ValidRequest_Returns201AndTempIdMap()
    {
        var (dbmsId, intTypeId) = await SeedDbmsAndTypeAsync();

        var request = BuildTwoTableRequest(dbmsId, intTypeId);

        var response = await SchemaBuilderClient.CreateAsync(request);

        response.TargetDbId.ShouldNotBe(Guid.Empty);
        response.Tables.Count.ShouldBe(2);

        var usersMap = response.Tables.Single(t => t.TempId == "t_users");
        usersMap.Id.ShouldNotBe(Guid.Empty);
        usersMap.Columns.Count.ShouldBe(1);
        usersMap.Columns[0].TempId.ShouldBe("c_uid");
        usersMap.Columns[0].Id.ShouldNotBe(Guid.Empty);

        var ordersMap = response.Tables.Single(t => t.TempId == "t_orders");
        ordersMap.Columns.Count.ShouldBe(2);
    }

    // ── E2: проверка персистентности в БД ───────────────────────────────────

    [Fact(DisplayName = "E2: создание схемы → TargetDb, MetaTables, MetaAttributes сохранены в БД")]
    public async Task CreateAsync_ValidRequest_PersistsEntities()
    {
        var (dbmsId, intTypeId) = await SeedDbmsAndTypeAsync();
        var request = BuildTwoTableRequest(dbmsId, intTypeId);

        var response = await SchemaBuilderClient.CreateAsync(request);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var targetDb = await db.TargetDbs.FindAsync(response.TargetDbId);
        targetDb.ShouldNotBeNull();
        targetDb!.DbName.ShouldBe(request.SchemaName);
        targetDb.DbmsId.ShouldBe(dbmsId);

        var tables = db.MetaTables.Where(t => t.TargetDbId == response.TargetDbId).ToList();
        tables.Count.ShouldBe(2);

        var usersTable = tables.Single(t => t.TableName == "users");
        var attrs = db.MetaAttributes.Where(a => a.MetaTableId == usersTable.Id).ToList();
        attrs.Count.ShouldBe(1);
        attrs[0].AttributeName.ShouldBe("id");
        attrs[0].IsPrimaryKey.ShouldBeTrue();
    }

    // ── E3: связи сохранены в БД ─────────────────────────────────────────────

    [Fact(DisplayName = "E3: создание схемы с FK → MetaRelationship сохранён в БД")]
    public async Task CreateAsync_WithRelationship_PersistsRelationship()
    {
        var (dbmsId, intTypeId) = await SeedDbmsAndTypeAsync();
        var request = BuildTwoTableRequest(dbmsId, intTypeId);

        var response = await SchemaBuilderClient.CreateAsync(request);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var colMaps = response.Tables.SelectMany(t => t.Columns).ToList();
        var srcId = colMaps.Single(c => c.TempId == "c_uid_fk").Id;
        var tgtId = colMaps.Single(c => c.TempId == "c_uid").Id;

        var rel = db.MetaRelationships.SingleOrDefault(r =>
            r.SourceAttributeId == srcId && r.TargetAttributeId == tgtId);
        rel.ShouldNotBeNull();
        rel!.RelationshipName.ShouldBe("fk_orders_users");
        rel.DeleteRule.ShouldBe("CASCADE");
    }

    // ── E4: дублирующееся имя схемы для той же СУБД → 409 ───────────────────

    [Fact(DisplayName = "E4: создание дубля имени схемы для той же СУБД → ConflictException (409)")]
    public async Task CreateAsync_DuplicateSchemaName_ThrowsConflictException()
    {
        var (dbmsId, intTypeId) = await SeedDbmsAndTypeAsync();
        var request = BuildTwoTableRequest(dbmsId, intTypeId);

        await SchemaBuilderClient.CreateAsync(request);

        var ex = await Should.ThrowAsync<ConflictException>(
            () => SchemaBuilderClient.CreateAsync(request));
        ex.StatusCode.ShouldBe(409);
    }

    // ── E5: несуществующий DbmsId → 409 ─────────────────────────────────────

    [Fact(DisplayName = "E5: несуществующий DbmsId → ConflictException (409)")]
    public async Task CreateAsync_UnknownDbmsId_ThrowsConflictException()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "items", [new ColumnDraft("c1", "id", Guid.NewGuid(), true, true, 0, [])])],
            []);

        await Should.ThrowAsync<ConflictException>(() => SchemaBuilderClient.CreateAsync(request));
    }

    // ── E6: структурные ошибки → 400 ─────────────────────────────────────────

    [Fact(DisplayName = "E6: дублирующийся TempId колонки → ValidationException (400)")]
    public async Task CreateAsync_DuplicateColumnTempId_Throws400()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [
                new TableDraft("t1", "a", [
                    new ColumnDraft("c_dup", "id", Guid.NewGuid(), true, true, 0, []),
                    new ColumnDraft("c_dup", "name", Guid.NewGuid(), false, true, 1, [])
                ])
            ],
            []);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.CreateAsync(request));
        ex.StatusCode.ShouldBe(400);
        ex.Errors.ShouldContainKey("Tables");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(Guid dbmsId, Guid intTypeId)> SeedDbmsAndTypeAsync()
    {
        var dbms = await DbmsDictionaryClient.CreateAsync(new CreateDbmsDictionaryRequest(
            DbmsName: "Postgres_CS_" + Uid(),
            DbmsSystemName: "postgres",
            DockerImage: "postgres:15-alpine",
            DefaultPort: 5432,
            EnvUserKey: "POSTGRES_USER",
            EnvPasswordKey: "POSTGRES_PASSWORD",
            EnvDatabaseKey: "POSTGRES_DB",
            ExtraEnvConfig: null,
            DefaultDatabase: "testdb",
            DefaultUsername: "user",
            DefaultPassword: "pass"));

        var physType = await PhysicalTypeClient.CreateAsync(
            new CreatePhysicalTypeRequest(dbms.Id, "INTEGER"));

        return (dbms.Id, physType.Id);
    }

    private static CreateSchemaRequest BuildTwoTableRequest(Guid dbmsId, Guid intTypeId) =>
        new(
            DbmsId: dbmsId,
            SchemaName: "test_schema_" + Uid(),
            Tables: [
                new TableDraft("t_users", "users", [
                    new ColumnDraft("c_uid", "id", intTypeId, true, true, 0, [])
                ]),
                new TableDraft("t_orders", "orders", [
                    new ColumnDraft("c_oid", "id", intTypeId, true, true, 0, []),
                    new ColumnDraft("c_uid_fk", "user_id", intTypeId, false, true, 1, [])
                ])
            ],
            Relationships: [
                new RelationshipDraft("fk_orders_users", "c_uid_fk", "c_uid", "CASCADE", null)
            ]);

    private static string Uid() => Guid.NewGuid().ToString("N")[..8];
}
