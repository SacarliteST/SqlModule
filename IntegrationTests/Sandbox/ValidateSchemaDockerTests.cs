using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Client.SchemaBuilder;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Sandbox;

[Collection(DockerTestCollection.Name)]
public sealed class ValidateSchemaDockerTests
{
    private readonly DockerTestApplication app;
    private readonly ISchemaBuilderClient client;

    public ValidateSchemaDockerTests(DockerTestApplication app)
    {
        this.app = app;
        client = app.SchemaBuilderClient;
    }

    // ── D1: Postgres валидная схема ──────────────────────────────────────────

    [Fact(DisplayName = "D1: Postgres → валидная схема users+orders с FK → 204")]
    public async Task ValidateAsync_ValidPostgresSchema_Returns204()
    {
        var (dbmsId, intTypeId) = await SeedAsync("postgres", "postgres:15-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null);

        var request = BuildTwoTableRequest(dbmsId, intTypeId);

        await Should.NotThrowAsync(() => client.ValidateAsync(request));
    }

    // ── D2: MySQL валидная схема ─────────────────────────────────────────────

    [Fact(DisplayName = "D2: MySQL → валидная схема users+orders с FK → 204")]
    public async Task ValidateAsync_ValidMySqlSchema_Returns204()
    {
        var (dbmsId, intTypeId) = await SeedAsync("mysql", "mysql:8.0", 3306,
            "MYSQL_USER", "MYSQL_PASSWORD", "MYSQL_DATABASE", "MYSQL_ROOT_PASSWORD=rootpass");

        var request = BuildTwoTableRequest(dbmsId, intTypeId);

        await Should.NotThrowAsync(() => client.ValidateAsync(request));
    }

    // ── D3: невалидный тип DDL → 422 ────────────────────────────────────────

    [Fact(DisplayName = "D3: невалидный SQL-тип → sandbox бросает ValidationException (422)")]
    public async Task ValidateAsync_InvalidDdlType_ThrowsValidationException()
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

        var request = new CreateSchemaRequest(
            dbms.Id, "s",
            [new TableDraft("t1", "items", [new ColumnDraft("c1", "id", badType.Id, true, true, 0, [])])],
            []);

        var ex = await Should.ThrowAsync<ValidationException>(() => client.ValidateAsync(request));
        ex.StatusCode.ShouldBe(422);
    }

    // ── D4: несуществующий DbmsId → 409 ─────────────────────────────────────

    [Fact(DisplayName = "D4: несуществующий DbmsId → ConflictException (409)")]
    public async Task ValidateAsync_UnknownDbmsId_ThrowsConflictException()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "items", [new ColumnDraft("c1", "id", Guid.NewGuid(), true, true, 0, [])])],
            []);

        await Should.ThrowAsync<ConflictException>(() => client.ValidateAsync(request));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(Guid dbmsId, Guid intTypeId)> SeedAsync(
        string systemName, string dockerImage, int port,
        string envUser, string envPassword, string envDatabase, string? extraEnv)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbms = DbmsDictionary.Create(
            $"{systemName}_Schema_{Uid()}", systemName, dockerImage, port,
            envUser, envPassword, envDatabase, extraEnv,
            "testdb", "user", "pass");
        db.DbmsDictionaries.Add(dbms);

        var intType = PhysicalType.Create(dbms.Id, "INTEGER");
        db.PhysicalTypes.Add(intType);

        await db.SaveChangesAsync();
        return (dbms.Id, intType.Id);
    }

    private static CreateSchemaRequest BuildTwoTableRequest(Guid dbmsId, Guid intTypeId) =>
        new(
            DbmsId: dbmsId,
            SchemaName: "test_schema",
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
