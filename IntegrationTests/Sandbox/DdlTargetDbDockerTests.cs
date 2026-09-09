using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Sandbox;

[Collection(DockerTestCollection.Name)]
[Trait("Category", DockerTestCollection.Category)]
public sealed class DdlTargetDbDockerTests(DockerTestApplication app)
{
    [Theory(DisplayName = "DDL: PostgreSQL/MySQL → validate, introspection и create согласованы")]
    [InlineData("postgres", "postgres:15-alpine", 5432, "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null)]
    [InlineData("mysql", "mysql:8.0", 3306, "MYSQL_USER", "MYSQL_PASSWORD", "MYSQL_DATABASE", "MYSQL_ROOT_PASSWORD=rootpass")]
    public async Task DdlWorkflow_RealSandbox_RestoresMetadata(
        string systemName, string image, int port,
        string envUser, string envPassword, string envDatabase, string? extraEnv)
    {
        var dbmsId = await SeedAsync(systemName, image, port, envUser, envPassword, envDatabase, extraEnv);
        var ddl = """
                  CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
                  CREATE TABLE orders (
                    id INTEGER PRIMARY KEY,
                    user_id INTEGER NOT NULL,
                    CONSTRAINT fk_orders_users FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
                  );
                  """;
        var validation = await app.SchemaBuilderClient.ValidateTargetDbDdlAsync(new ValidateTargetDbDdlRequest
        {
            DbmsId = dbmsId,
            DbName = "validate_ddl",
            DdlScript = ddl
        });
        validation.DetectedTables.ShouldBe(2);
        validation.DetectedRelationships.ShouldBe(1);

        var created = await app.SchemaBuilderClient.CreateTargetDbFromDdlAsync(
            Guid.NewGuid().ToString(), new CreateTargetDbFromDdlRequest
            {
                DbmsId = dbmsId,
                DbName = "ddl_" + Guid.NewGuid().ToString("N"),
                Description = "From DDL",
                IsReadOnly = false,
                DdlScript = ddl
            });
        created.Schema.Tables.Count.ShouldBe(2);
        created.Schema.Relationships.Count.ShouldBe(1);
        created.Schema.Tables.SelectMany(x => x.Columns).Count().ShouldBe(4);
    }

    [Fact(DisplayName = "DDL: PostgreSQL → учебные типы и их параметры восстанавливаются из реального каталога")]
    public async Task PostgreSqlDdl_EducationalTypes_RestoreMetadata()
    {
        var dbmsId = await SeedAsync(
            "postgres", "postgres:15-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var varchar = PhysicalType.Create(dbmsId, "character varying");
            var numeric = PhysicalType.Create(dbmsId, "numeric");
            db.AddRange(
                varchar,
                numeric,
                PhysicalType.Create(dbmsId, "boolean"),
                PhysicalType.Create(dbmsId, "date"),
                ParameterDefinition.Create(
                    varchar.Id, "length", "Длина", "number", "255", 0, "({value})", true,
                    null, null, null),
                ParameterDefinition.Create(
                    numeric.Id, "precision", "Точность", "number", "18", 0, "({value}", true,
                    null, null, null),
                ParameterDefinition.Create(
                    numeric.Id, "scale", "Масштаб", "number", "2", 1, ", {value})", true,
                    null, null, null));
            await db.SaveChangesAsync();
        }

        var ddl = """
                  CREATE TABLE products (
                    id INTEGER PRIMARY KEY,
                    description TEXT,
                    name VARCHAR(200) NOT NULL,
                    is_active BOOLEAN NOT NULL,
                    created_on DATE NOT NULL,
                    price NUMERIC(12, 2) NOT NULL
                  );
                  """;
        var validation = await app.SchemaBuilderClient.ValidateTargetDbDdlAsync(new ValidateTargetDbDdlRequest
        {
            DbmsId = dbmsId,
            DbName = "validate_postgres_types",
            DdlScript = ddl
        });
        validation.DetectedTables.ShouldBe(1);

        var created = await app.SchemaBuilderClient.CreateTargetDbFromDdlAsync(
            Guid.NewGuid().ToString(), new CreateTargetDbFromDdlRequest
            {
                DbmsId = dbmsId,
                DbName = "postgres_types_" + Guid.NewGuid().ToString("N"),
                Description = "PostgreSQL types",
                IsReadOnly = false,
                DdlScript = ddl
            });

        var columns = created.Schema.Tables.ShouldHaveSingleItem().Columns;
        columns.Single(column => column.Name == "name")
            .Parameters.ShouldHaveSingleItem().Value.ShouldBe("200");
        columns.Single(column => column.Name == "price").Parameters
            .OrderBy(parameter => parameter.ParameterKey)
            .Select(parameter => parameter.Value).ShouldBe(["12", "2"]);
    }

    private async Task<Guid> SeedAsync(
        string systemName, string image, int port,
        string envUser, string envPassword, string envDatabase, string? extraEnv)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DbmsDictionary.Create(
            $"{systemName}_Ddl_{Guid.NewGuid():N}", systemName, image, port,
            envUser, envPassword, envDatabase, extraEnv, "testdb", "user", "pass");
        db.AddRange(dbms, PhysicalType.Create(dbms.Id, "INTEGER"), PhysicalType.Create(dbms.Id, "TEXT"));
        await db.SaveChangesAsync();
        return dbms.Id;
    }
}
