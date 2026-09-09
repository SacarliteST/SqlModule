using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal interface ISchemaDiffApplier
{
    Task ApplyAsync(Guid targetDbId, SchemaUpsertRequest request, CancellationToken ct);
}

internal sealed class SchemaDiffApplier(AppDbContext db) : ISchemaDiffApplier
{
    public async Task ApplyAsync(Guid targetDbId, SchemaUpsertRequest request, CancellationToken ct)
    {
        var tables = await db.MetaTables.Where(x => x.TargetDbId == targetDbId).ToListAsync(ct);
        var tableIds = tables.Select(x => x.Id).ToList();
        var columns = await db.MetaAttributes.Where(x => tableIds.Contains(x.MetaTableId)).ToListAsync(ct);
        var columnIds = columns.Select(x => x.Id).ToList();
        var parameters = await db.AttributeParameterValues
            .Where(x => columnIds.Contains(x.MetaAttributeId)).ToListAsync(ct);
        var relationships = await db.MetaRelationships
            .Where(x => columnIds.Contains(x.SourceAttributeId) || columnIds.Contains(x.TargetAttributeId))
            .ToListAsync(ct);

        var requestedTableIds = request.Tables!.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        var requestedColumnIds = request.Tables!.SelectMany(x => x.Columns!).Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value).ToHashSet();
        var requestedRelationshipIds = request.Relationships!.Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value).ToHashSet();
        var removedColumns = columns.Where(x => !requestedColumnIds.Contains(x.Id)).ToList();
        var removedColumnIds = removedColumns.Select(x => x.Id).ToHashSet();

        db.MetaRelationships.RemoveRange(relationships.Where(x =>
            !requestedRelationshipIds.Contains(x.Id) || removedColumnIds.Contains(x.SourceAttributeId) ||
            removedColumnIds.Contains(x.TargetAttributeId)));
        db.AttributeParameterValues.RemoveRange(parameters.Where(x => removedColumnIds.Contains(x.MetaAttributeId)));
        if (removedColumnIds.Count > 0)
        {
            var emptyCells = await db.CellValues.Where(x => removedColumnIds.Contains(x.MetaAttributeId)).ToListAsync(ct);
            db.CellValues.RemoveRange(emptyCells);
        }
        db.MetaAttributes.RemoveRange(removedColumns);
        db.MetaTables.RemoveRange(tables.Where(x => !requestedTableIds.Contains(x.Id)));

        var columnIdByRef = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var tableDraft in request.Tables!)
        {
            MetaTable table;
            if (tableDraft.Id.HasValue)
            {
                table = tables.Single(x => x.Id == tableDraft.Id.Value);
                table.Update(tableDraft.Name!, tableDraft.Description, tableDraft.SortOrder);
            }
            else
            {
                table = MetaTable.Create(targetDbId, tableDraft.Name!, tableDraft.Description,
                    sortOrder: tableDraft.SortOrder!.Value);
                db.MetaTables.Add(table);
            }

            foreach (var columnDraft in tableDraft.Columns!)
            {
                MetaAttribute column;
                if (columnDraft.Id.HasValue)
                {
                    column = columns.Single(x => x.Id == columnDraft.Id.Value);
                    column.UpdateSchema(columnDraft.Name!, columnDraft.PhysicalTypeId!.Value,
                        columnDraft.IsPrimaryKey!.Value, columnDraft.IsRequired!.Value,
                        columnDraft.SortOrder!.Value);
                }
                else
                {
                    column = MetaAttribute.Create(table.Id, columnDraft.PhysicalTypeId!.Value,
                        columnDraft.Name!, columnDraft.IsPrimaryKey!.Value, columnDraft.IsRequired!.Value,
                        columnDraft.SortOrder!.Value);
                    db.MetaAttributes.Add(column);
                }

                columnIdByRef[SchemaUpsertMapper.Key(columnDraft.Id, columnDraft.TempId)] = column.Id;
                SyncParameters(column.Id, columnDraft.Parameters!, parameters);
            }
        }

        foreach (var relationshipDraft in request.Relationships!)
        {
            var sourceId = columnIdByRef[relationshipDraft.SourceColumnRef!];
            var targetId = columnIdByRef[relationshipDraft.TargetColumnRef!];
            if (relationshipDraft.Id.HasValue)
            {
                relationships.Single(x => x.Id == relationshipDraft.Id.Value).UpdateSchema(
                    relationshipDraft.Name!, sourceId, targetId,
                    relationshipDraft.DeleteRule, relationshipDraft.UpdateRule);
            }
            else
            {
                db.MetaRelationships.Add(MetaRelationship.Create(
                    relationshipDraft.Name!, sourceId, targetId,
                    relationshipDraft.DeleteRule, relationshipDraft.UpdateRule));
            }
        }
    }

    private void SyncParameters(
        Guid columnId,
        IReadOnlyList<SchemaColumnParameterDraft> requested,
        IReadOnlyList<AttributeParameterValue> allParameters)
    {
        var existing = allParameters.Where(x => x.MetaAttributeId == columnId).ToList();
        var requestedIds = requested.Select(x => x.ParameterDefinitionId!.Value).ToHashSet();
        db.AttributeParameterValues.RemoveRange(existing.Where(x => !requestedIds.Contains(x.ParameterDefinitionId)));
        foreach (var draft in requested)
        {
            var value = existing.SingleOrDefault(x => x.ParameterDefinitionId == draft.ParameterDefinitionId);
            if (value is null)
            {
                db.AttributeParameterValues.Add(AttributeParameterValue.Create(
                    columnId, draft.ParameterDefinitionId!.Value, draft.Value!));
            }
            else if (value.ParameterValue != draft.Value)
            {
                value.Update(draft.Value!);
            }
        }
    }
}
