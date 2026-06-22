using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Sandbox;

[Collection(DockerTestCollection.Name)]
[Trait("Category", DockerTestCollection.Category)]
public sealed class CreateSchemaDockerTests
{
    private readonly DockerTestApplication app;

    public CreateSchemaDockerTests(DockerTestApplication app)
    {
        this.app = app;
    }

    // ── CD1: Postgres → 201 + мета сохранена ────────────────────────────────

    [Fact(DisplayName = "CD1: Postgres → валидная схема → 201, TargetDb сохранена")]
    public async Task CreateAsync_ValidPostgresSchema_Returns201AndPersists()
    {
        var (dbmsId, intTypeId) = await SeedAsync("postgres", "postgres:15-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null);

        var schemaName = "docker_schema_" + Uid();
        var request = BuildTwoTableRequest(dbmsId, intTypeId, schemaName);

        var response = await app.SchemaBuilderClient.CreateAsync(request);

        response.TargetDbId.ShouldNotBe(Guid.Empty);
        response.Tables.Count.ShouldBe(2);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.TargetDbs.FindAsync(response.TargetDbId);
        saved.ShouldNotBeNull();
        saved!.DbName.ShouldBe(schemaName);
    }

    // ── CD2: невалидный DDL → 422 + ничего не сохранено ────────────────────

    [Fact(DisplayName = "CD2: невалидный SQL-тип → 422, TargetDb не создана")]
    public async Task CreateAsync_InvalidDdlType_Throws422AndDoesNotPersist()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbms = DbmsDictionary.Create(
            "Postgres_BadType_" + Uid(), "postgres", "postgres:15-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);

        var badType = PhysicalType.Create(dbms.Id, "NOT_A_VALID_SQL_TYPE");
        db.PhysicalTypes.Add(badType);
        await db.SaveChangesAsync();

        var schemaName = "should_not_exist_" + Uid();
        var request = new CreateSchemaRequest(
            dbms.Id, schemaName,
            [new TableDraft("t1", "items", [new ColumnDraft("c1", "id", badType.Id, true, true, 0, [])])],
            []);

        var ex = await Should.ThrowAsync<ValidationException>(() => app.SchemaBuilderClient.CreateAsync(request));
        ex.StatusCode.ShouldBe(422);

        var persisted = db.TargetDbs.Any(t => t.DbmsId == dbms.Id && t.DbName == schemaName);
        persisted.ShouldBeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(Guid dbmsId, Guid intTypeId)> SeedAsync(
        string systemName, string dockerImage, int port,
        string envUser, string envPassword, string envDatabase, string? extraEnv)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbms = DbmsDictionary.Create(
            $"{systemName}_CreateSchema_{Uid()}", systemName, dockerImage, port,
            envUser, envPassword, envDatabase, extraEnv,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);

        var intType = PhysicalType.Create(dbms.Id, "INTEGER");
        db.PhysicalTypes.Add(intType);

        await db.SaveChangesAsync();
        return (dbms.Id, intType.Id);
    }

    private static CreateSchemaRequest BuildTwoTableRequest(Guid dbmsId, Guid intTypeId, string schemaName) =>
        new(
            DbmsId: dbmsId,
            SchemaName: schemaName,
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
