using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Isolated;

namespace SQLModule.IntegrationTests.Schema.SchemaBuilder;

[Collection(IntegrationTestCollection.Name)]
public sealed class DemoPhysicalTypeTests(TestApplication app) : ApiTestBase(app)
{
    private static readonly Guid DemoDbmsId = new("10000000-0000-0000-0000-000000000001");

    [Fact(DisplayName = "PostgreSQL Demo → каталог типов одинаково работает для DDL и редактора")]
    public async Task DemoCatalog_SupportsEducationalPostgreSqlTypes()
    {
        await SeedDemoTwiceAsync();
        await ReplaceTextTypeWithExistingCanonicalTypeAsync();
        await SeedDemoTwiceAsync();

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var types = await db.PhysicalTypes.AsNoTracking()
                .Include(type => type.ParameterDefinitions)
                .Where(type => type.DbmsId == DemoDbmsId)
                .ToListAsync();

            types.Select(type => type.TypeName).OrderBy(name => name).ShouldBe([
                "boolean", "character varying", "date", "integer", "numeric", "text"
            ]);
            types.Single(type => type.TypeName == "character varying")
                .ParameterDefinitions.ShouldHaveSingleItem().ParameterKey.ShouldBe("length");
            types.Single(type => type.TypeName == "numeric")
                .ParameterDefinitions.OrderBy(parameter => parameter.SortOrder)
                .Select(parameter => parameter.ParameterKey).ShouldBe(["precision", "scale"]);
        }

        var fake = App.Services.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<FakeSandboxExecutor>();
        fake.OverrideInspection = new InspectedSchema([
            new InspectedTable("demo_types", [
                new InspectedColumn("id", "integer", false, true, 0, null, 32, 0),
                new InspectedColumn("description", "text", true, false, 1, null, null, null),
                new InspectedColumn("name", "character varying", false, false, 2, 200, null, null),
                new InspectedColumn("is_active", "boolean", false, false, 3, null, null, null),
                new InspectedColumn("created_on", "date", false, false, 4, null, null, null),
                new InspectedColumn("amount", "numeric", false, false, 5, null, 12, 2)
            ])
        ], []);

        try
        {
            AsTeacher();
            var ddl = """
                      CREATE TABLE demo_types (
                        id INTEGER PRIMARY KEY,
                        description TEXT,
                        name VARCHAR(200) NOT NULL,
                        is_active BOOLEAN NOT NULL,
                        created_on DATE NOT NULL,
                        amount NUMERIC(12, 2) NOT NULL
                      );
                      """;
            var validation = await SchemaBuilderClient.ValidateTargetDbDdlAsync(new ValidateTargetDbDdlRequest
            {
                DbmsId = DemoDbmsId,
                DbName = "demo_types_validation",
                DdlScript = ddl
            });
            validation.DetectedTables.ShouldBe(1);

            var created = await SchemaBuilderClient.CreateTargetDbFromDdlAsync(
                Guid.NewGuid().ToString(), new CreateTargetDbFromDdlRequest
                {
                    DbmsId = DemoDbmsId,
                    DbName = "demo_types_" + Guid.NewGuid().ToString("N"),
                    Description = "PostgreSQL educational types",
                    IsReadOnly = false,
                    DdlScript = ddl
                });

            var columns = created.Schema.Tables.ShouldHaveSingleItem().Columns;
            columns.Select(column => column.PhysicalTypeName).ShouldBe([
                "integer", "text", "character varying", "boolean", "date", "numeric"
            ]);
            columns.Single(column => column.Name == "name")
                .Parameters.ShouldHaveSingleItem().Value.ShouldBe("200");
            columns.Single(column => column.Name == "amount").Parameters
                .OrderBy(parameter => parameter.ParameterKey)
                .Select(parameter => parameter.Value).ShouldBe(["12", "2"]);
        }
        finally
        {
            fake.OverrideInspection = null;
        }
    }

    private async Task ReplaceTextTypeWithExistingCanonicalTypeAsync()
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existingText = await db.PhysicalTypes
            .SingleOrDefaultAsync(type => type.DbmsId == DemoDbmsId && type.TypeName == "text");

        if (existingText is not null)
        {
            db.PhysicalTypes.Remove(existingText);
            await db.SaveChangesAsync();
        }

        db.PhysicalTypes.Add(PhysicalType.Create(DemoDbmsId, "text"));
        await db.SaveChangesAsync();
    }

    private async Task SeedDemoTwiceAsync()
    {
        for (var iteration = 0; iteration < 2; iteration++)
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await new DemoDataSeeder(db).SeedAsync();
        }
    }
}
