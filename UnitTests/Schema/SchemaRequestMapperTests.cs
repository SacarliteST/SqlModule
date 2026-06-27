using System.Reflection;
using Shouldly;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.UnitTests.Schema;

public sealed class SchemaRequestMapperTests
{
    [Fact]
    public void ToSchemaSpec_MapsSchemaNameAndTableTempIds()
    {
        var ptId = Guid.NewGuid();
        var pt = BuildPhysicalType(ptId, "INTEGER", []);
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "my_schema",
            [new TableDraft("tbl-1", "users", [
                new ColumnDraft("col-1", "id", ptId, true, true, 0, [])
            ])],
            []);

        var spec = SchemaRequestMapper.ToSchemaSpec(request, new Dictionary<Guid, PhysicalType> { [ptId] = pt });

        spec.SchemaName.ShouldBe("my_schema");
        spec.Tables.ShouldHaveSingleItem();
        spec.Tables[0].Key.ShouldBe("tbl-1");
        spec.Tables[0].Name.ShouldBe("users");
        spec.Tables[0].Columns[0].Key.ShouldBe("col-1");
    }

    [Fact]
    public void ToSchemaSpec_ResolvesSqlTypeViaPhysicalType()
    {
        var ptId = Guid.NewGuid();
        var paramDefId = Guid.NewGuid();
        var pt = BuildPhysicalType(ptId, "VARCHAR", [
            BuildParameterDef(paramDefId, ptId, "({value})", valuePrefix: null, valueSuffix: null, required: false, defaultValue: null, sortOrder: 0)
        ]);
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "items", [
                new ColumnDraft("c1", "name", ptId, false, true, 0, [
                    new ColumnParameterDraft(paramDefId, "255")
                ])
            ])],
            []);

        var spec = SchemaRequestMapper.ToSchemaSpec(request, new Dictionary<Guid, PhysicalType> { [ptId] = pt });

        spec.Tables[0].Columns[0].SqlType.ShouldBe("VARCHAR(255)");
    }

    [Fact]
    public void ToSchemaSpec_MapsIsNullableFromIsRequired()
    {
        var ptId = Guid.NewGuid();
        var pt = BuildPhysicalType(ptId, "TEXT", []);
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "t", [
                new ColumnDraft("c1", "required_col", ptId, false, true, 0, []),
                new ColumnDraft("c2", "nullable_col", ptId, false, false, 1, [])
            ])],
            []);

        var spec = SchemaRequestMapper.ToSchemaSpec(request, new Dictionary<Guid, PhysicalType> { [ptId] = pt });

        spec.Tables[0].Columns[0].IsNullable.ShouldBeFalse();
        spec.Tables[0].Columns[1].IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void ToSchemaSpec_MapsRelationshipTempIds()
    {
        var ptId = Guid.NewGuid();
        var pt = BuildPhysicalType(ptId, "INT", []);
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [
                new TableDraft("t1", "a", [new ColumnDraft("c1", "id", ptId, true, true, 0, [])]),
                new TableDraft("t2", "b", [
                    new ColumnDraft("c2", "id", ptId, true, true, 0, []),
                    new ColumnDraft("c3", "a_id", ptId, false, true, 1, [])
                ])
            ],
            [new RelationshipDraft("fk_b_a", "c3", "c1", "CASCADE", null)]);

        var pts = new Dictionary<Guid, PhysicalType> { [ptId] = pt };
        var spec = SchemaRequestMapper.ToSchemaSpec(request, pts);

        spec.Relationships.ShouldHaveSingleItem();
        spec.Relationships[0].SourceColumnKey.ShouldBe("c3");
        spec.Relationships[0].TargetColumnKey.ShouldBe("c1");
        spec.Relationships[0].DeleteRule.ShouldBe("CASCADE");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static PhysicalType BuildPhysicalType(
        Guid id, string typeName, IReadOnlyList<ParameterDefinition> paramDefs)
    {
        var pt = PhysicalType.Create(Guid.NewGuid(), typeName, id);
        var field = typeof(PhysicalType)
            .GetField("parameterDefinitions", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (List<ParameterDefinition>)field.GetValue(pt)!;
        list.AddRange(paramDefs);
        return pt;
    }

    private static ParameterDefinition BuildParameterDef(
        Guid id, Guid physicalTypeId, string sqlFragment,
        string? valuePrefix, string? valueSuffix,
        bool required, string? defaultValue, short sortOrder)
        => ParameterDefinition.Create(
            physicalTypeId,
            "KEY", "DisplayName", "text",
            defaultValue, sortOrder, sqlFragment, required,
            valuePrefix, valueSuffix, null,
            id);
}
