using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal static class SchemaUpsertMapper
{
    internal static CreateSchemaRequest ToCreateRequest(
        Guid dbmsId,
        string schemaName,
        SchemaUpsertRequest request)
    {
        return new CreateSchemaRequest(
            dbmsId,
            schemaName,
            request.Tables!.Select(table => new TableDraft(
                Key(table.Id, table.TempId),
                table.Name!,
                table.Columns!.Select(column => new ColumnDraft(
                    Key(column.Id, column.TempId),
                    column.Name!,
                    column.PhysicalTypeId!.Value,
                    column.IsPrimaryKey!.Value,
                    column.IsRequired!.Value,
                    column.SortOrder!.Value,
                    column.Parameters!.Select(parameter => new ColumnParameterDraft(
                        parameter.ParameterDefinitionId!.Value,
                        parameter.Value!)).ToList())).ToList())).ToList(),
            request.Relationships!.Select(relationship => new RelationshipDraft(
                relationship.Name!,
                relationship.SourceColumnRef!,
                relationship.TargetColumnRef!,
                relationship.DeleteRule,
                relationship.UpdateRule)).ToList());
    }

    internal static Guid EntityId(Guid? id) => id ?? Guid.NewGuid();

    internal static string Key(Guid? id, string? tempId) => id?.ToString() ?? tempId!;
}
