using SQLModule.Domain.Schema;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Sandbox;

/// <summary>
/// Маппинг сохранённых мета-данных (<see cref="MetaTable"/>, <see cref="MetaRelationship"/>)
/// на нейтральные модели <see cref="SchemaSpec"/> и <see cref="DataSpec"/>.
/// </summary>
internal static class MetaSchemaMapper
{
    /// <summary>
    /// Строит <see cref="SchemaSpec"/> из набора мета-таблиц и связей.
    /// Каждый <see cref="MetaAttribute"/> должен иметь загруженный <c>PhysicalType</c> с <c>ParameterDefinitions</c>.
    /// </summary>
    internal static SchemaSpec ToSchemaSpec(
        IReadOnlyList<MetaTable> tables,
        IReadOnlyList<MetaRelationship> relationships,
        ILookup<Guid, AttributeParameterValue> parameterValues)
    {
        var tableSpecs = tables
            .Select(t => new TableSpec(
                Key: t.Id.ToString(),
                Name: t.TableName,
                Columns: t.Attributes
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new ColumnSpec(
                        Key: a.Id.ToString(),
                        Name: a.AttributeName,
                        SqlType: a.PhysicalType.ResolveSql(parameterValues[a.Id].ToList()),
                        IsPrimaryKey: a.IsPrimaryKey,
                        IsNullable: !a.IsRequired,
                        SortOrder: a.SortOrder))
                    .ToList()))
            .ToList();

        var relSpecs = relationships
            .Select(r => new RelationshipSpec(
                Name: r.RelationshipName,
                SourceColumnKey: r.SourceAttributeId.ToString(),
                TargetColumnKey: r.TargetAttributeId.ToString(),
                DeleteRule: r.DeleteRule,
                UpdateRule: r.UpdateRule))
            .ToList();

        return new SchemaSpec(String.Empty, tableSpecs, relSpecs);
    }

    /// <summary>Строит <see cref="DataSpec"/> из EAV-строк мета-таблиц.</summary>
    internal static DataSpec ToDataSpec(IReadOnlyList<MetaTable> tables)
    {
        var tableRows = tables
            .Select(t => new TableRows(
                TableKey: t.Id.ToString(),
                Rows: t.DataRecords
                    .OrderBy(dr => dr.SortOrder)
                    .Select(dr => new RowSpec(
                        Cells: dr.CellValues
                            .Select(cv => new CellSpec(
                                ColumnKey: cv.MetaAttributeId.ToString(),
                                Value: cv.TextValue))
                            .ToList()))
                    .ToList()))
            .ToList();

        return new DataSpec(tableRows);
    }
}
