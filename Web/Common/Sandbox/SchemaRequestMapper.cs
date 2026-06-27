using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Sandbox;

/// <summary>
/// Маппинг <see cref="CreateSchemaRequest"/> → <see cref="SchemaSpec"/>.
/// Принимает предзагруженные <see cref="PhysicalType"/> с <c>ParameterDefinitions</c>;
/// загрузку из БД выполняет обработчик команды.
/// </summary>
internal static class SchemaRequestMapper
{
    internal static SchemaSpec ToSchemaSpec(
        CreateSchemaRequest request,
        IReadOnlyDictionary<Guid, PhysicalType> physicalTypes)
    {
        var tables = request.Tables
            .Select(t => new TableSpec(
                Key: t.TempId,
                Name: t.Name,
                Columns: t.Columns
                    .Select(c =>
                    {
                        var pt = physicalTypes[c.PhysicalTypeId];
                        var values = c.Parameters
                            .Select(p => AttributeParameterValue.CreateInternal(p.Value, p.ParameterDefinitionId))
                            .ToList();
                        return new ColumnSpec(
                            Key: c.TempId,
                            Name: c.Name,
                            SqlType: pt.ResolveSql(values),
                            IsPrimaryKey: c.IsPrimaryKey,
                            IsNullable: !c.IsRequired,
                            SortOrder: c.SortOrder);
                    })
                    .ToList()))
            .ToList();

        var relationships = request.Relationships
            .Select(r => new RelationshipSpec(
                Name: r.Name,
                SourceColumnKey: r.SourceColumnTempId,
                TargetColumnKey: r.TargetColumnTempId,
                DeleteRule: r.DeleteRule,
                UpdateRule: r.UpdateRule))
            .ToList();

        return new SchemaSpec(request.SchemaName, tables, relationships);
    }
}
