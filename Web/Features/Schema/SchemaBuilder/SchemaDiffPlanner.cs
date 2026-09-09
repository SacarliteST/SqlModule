using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal sealed record SchemaDiffPlan(
    IReadOnlyList<SchemaChangeResponse> Changes,
    IReadOnlyList<SchemaChangeResponse> DestructiveChanges)
{
    internal bool IsSafe => DestructiveChanges.Count == 0;
}

internal interface ISchemaDiffPlanner
{
    Task<SchemaDiffPlan> BuildAsync(Guid targetDbId, SchemaUpsertRequest request, CancellationToken ct);
}

internal sealed class SchemaDiffPlanner(AppDbContext db) : ISchemaDiffPlanner
{
    public async Task<SchemaDiffPlan> BuildAsync(
        Guid targetDbId, SchemaUpsertRequest request, CancellationToken ct)
    {
        var currentTables = await db.MetaTables.AsNoTracking()
            .Where(x => x.TargetDbId == targetDbId).ToListAsync(ct);
        var tableIds = currentTables.Select(x => x.Id).ToList();
        var currentColumns = await db.MetaAttributes.AsNoTracking()
            .Where(x => tableIds.Contains(x.MetaTableId)).ToListAsync(ct);
        var columnIds = currentColumns.Select(x => x.Id).ToList();
        var currentParameters = await db.AttributeParameterValues.AsNoTracking()
            .Where(x => columnIds.Contains(x.MetaAttributeId)).ToListAsync(ct);
        var currentRelationships = await db.MetaRelationships.AsNoTracking()
            .Where(x => columnIds.Contains(x.SourceAttributeId) || columnIds.Contains(x.TargetAttributeId))
            .ToListAsync(ct);
        var rowCounts = await db.DataRecords.AsNoTracking().Where(x => tableIds.Contains(x.MetaTableId))
            .GroupBy(x => x.MetaTableId).Select(x => new { x.Key, Count = x.LongCount() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var valueCounts = await db.CellValues.AsNoTracking().Where(x => columnIds.Contains(x.MetaAttributeId))
            .GroupBy(x => x.MetaAttributeId).Select(x => new
            {
                x.Key,
                Count = x.Where(v => v.TextValue != null)
                    .Select(v => v.DataRecordId)
                    .Distinct()
                    .LongCount()
            }).ToDictionaryAsync(x => x.Key, ct);
        var cellValues = await db.CellValues.AsNoTracking()
            .Where(x => columnIds.Contains(x.MetaAttributeId) && x.TextValue != null)
            .Select(x => new { x.MetaAttributeId, x.TextValue }).ToListAsync(ct);
        var valuesByColumn = cellValues.ToLookup(x => x.MetaAttributeId, x => x.TextValue!);
        var requestedTypeIds = request.Tables!.SelectMany(x => x.Columns!).Select(x => x.PhysicalTypeId!.Value);
        var allTypeIds = currentColumns.Select(x => x.PhysicalTypeId).Concat(requestedTypeIds).Distinct().ToList();
        var typeNames = await db.PhysicalTypes.AsNoTracking().Where(x => allTypeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.TypeName, ct);

        var changes = new List<SchemaChangeResponse>();
        var destructive = new List<SchemaChangeResponse>();
        var requestedTables = request.Tables!.Where(x => x.Id.HasValue).ToDictionary(x => x.Id!.Value);
        var requestedColumns = request.Tables!.SelectMany(x => x.Columns!).Where(x => x.Id.HasValue)
            .ToDictionary(x => x.Id!.Value);
        var requestedRelationships = request.Relationships!.Where(x => x.Id.HasValue)
            .ToDictionary(x => x.Id!.Value);

        foreach (var table in request.Tables!)
        {
            if (!table.Id.HasValue)
            {
                changes.Add(Change("Add", "Table", null, table.TempId, TablePath(table),
                    $"Добавлена таблица '{table.Name}'."));
                continue;
            }

            var old = currentTables.Single(x => x.Id == table.Id.Value);
            if (old.TableName != table.Name || old.Description != table.Description || old.SortOrder != table.SortOrder)
            {
                changes.Add(Change("Update", "Table", old.Id, null, TablePath(table),
                    $"Изменена таблица '{old.TableName}'."));
            }
        }

        foreach (var old in currentTables.Where(x => !requestedTables.ContainsKey(x.Id)))
        {
            var rows = rowCounts.GetValueOrDefault(old.Id);
            var change = Change("Delete", "Table", old.Id, null, $"tables[{old.Id}]",
                $"Удалена таблица '{old.TableName}'.", rows,
                rows > 0 ? "Error" : "Info", rows > 0 ? "SchemaChangeBlockedByData" : null);
            (rows > 0 ? destructive : changes).Add(change);
        }

        foreach (var table in request.Tables)
        {
            var existingTableRows = table.Id.HasValue ? rowCounts.GetValueOrDefault(table.Id.Value) : 0;
            foreach (var column in table.Columns!)
            {
                var path = $"{TablePath(table)}.columns[{column.Id?.ToString() ?? column.TempId}]";
                if (!column.Id.HasValue)
                {
                    var change = Change("Add", "Column", null, column.TempId, path,
                        $"Добавлена колонка '{column.Name}'.", existingTableRows,
                        column.IsRequired == true && existingTableRows > 0 ? "Error" : "Info",
                        column.IsRequired == true && existingTableRows > 0 ? "RequiredColumnNeedsDefault" : null);
                    (change.Code is null ? changes : destructive).Add(change);
                    continue;
                }

                var old = currentColumns.Single(x => x.Id == column.Id.Value);
                var hasValues = valueCounts.GetValueOrDefault(old.Id)?.Count ?? 0;
                var nullOrMissingRows = Math.Max(0, existingTableRows - hasValues);
                string? code = null;
                if (old.IsPrimaryKey != column.IsPrimaryKey && existingTableRows > 0)
                {
                    code = "SchemaChangeBlockedByData";
                }
                else if (old.PhysicalTypeId != column.PhysicalTypeId && hasValues > 0 &&
                         valuesByColumn[old.Id].Any(x => !CanConvert(x, typeNames[column.PhysicalTypeId!.Value])))
                {
                    code = "ColumnTypeConversionFailed";
                }
                else if (!old.IsRequired && column.IsRequired == true && nullOrMissingRows > 0)
                {
                    code = "ColumnContainsNullValues";
                }
                else if (ParametersShrink(old.Id, column, currentParameters, valuesByColumn[old.Id]))
                {
                    code = "SchemaChangeBlockedByData";
                }

                if (old.AttributeName != column.Name || old.PhysicalTypeId != column.PhysicalTypeId ||
                    old.IsPrimaryKey != column.IsPrimaryKey || old.IsRequired != column.IsRequired ||
                    old.SortOrder != column.SortOrder || ParametersChanged(old.Id, column, currentParameters))
                {
                    var affectedRows = code == "ColumnContainsNullValues" ? nullOrMissingRows : hasValues;
                    var change = Change("Update", "Column", old.Id, null, path,
                        $"Изменена колонка '{old.AttributeName}'.", affectedRows,
                        code is null ? "Info" : "Error", code);
                    (code is null ? changes : destructive).Add(change);
                }
            }
        }

        foreach (var old in currentColumns.Where(x => !requestedColumns.ContainsKey(x.Id)))
        {
            var count = valueCounts.GetValueOrDefault(old.Id)?.Count ?? 0;
            var change = Change("Delete", "Column", old.Id, null, $"columns[{old.Id}]",
                $"Удалена колонка '{old.AttributeName}'.", count,
                count > 0 ? "Error" : "Info", count > 0 ? "SchemaChangeBlockedByData" : null);
            (count > 0 ? destructive : changes).Add(change);
        }

        foreach (var relationship in request.Relationships!)
        {
            var path = $"relationships[{relationship.Id?.ToString() ?? relationship.TempId}]";
            if (!relationship.Id.HasValue)
            {
                var hasConflict = Guid.TryParse(relationship.SourceColumnRef, out var sourceId) &&
                                  Guid.TryParse(relationship.TargetColumnRef, out var targetId) &&
                                  valuesByColumn[sourceId].Except(valuesByColumn[targetId], StringComparer.Ordinal).Any();
                var change = Change("Add", "Relationship", null, relationship.TempId, path,
                    $"Добавлена связь '{relationship.Name}'.", hasConflict ? valuesByColumn[sourceId].LongCount() : null,
                    hasConflict ? "Error" : "Info", hasConflict ? "RelationshipDataConflict" : null);
                (hasConflict ? destructive : changes).Add(change);
            }
            else
            {
                var old = currentRelationships.Single(x => x.Id == relationship.Id.Value);
                var changed = old.RelationshipName != relationship.Name || old.DeleteRule != relationship.DeleteRule ||
                              old.UpdateRule != relationship.UpdateRule ||
                              !RefMatches(relationship.SourceColumnRef!, old.SourceAttributeId) ||
                              !RefMatches(relationship.TargetColumnRef!, old.TargetAttributeId);
                if (changed)
                {
                    var affected = rowCounts.GetValueOrDefault(currentColumns.Single(x => x.Id == old.SourceAttributeId).MetaTableId);
                    var change = Change("Update", "Relationship", old.Id, null, path,
                        $"Изменена связь '{old.RelationshipName}'.", affected,
                        affected > 0 ? "Error" : "Info", affected > 0 ? "SchemaChangeBlockedByData" : null);
                    (affected > 0 ? destructive : changes).Add(change);
                }
            }
        }

        foreach (var old in currentRelationships.Where(x => !requestedRelationships.ContainsKey(x.Id)))
        {
            var affected = rowCounts.GetValueOrDefault(currentColumns.Single(x => x.Id == old.SourceAttributeId).MetaTableId);
            var change = Change("Delete", "Relationship", old.Id, null, $"relationships[{old.Id}]",
                $"Удалена связь '{old.RelationshipName}'.", affected,
                affected > 0 ? "Error" : "Info", affected > 0 ? "SchemaChangeBlockedByData" : null);
            (affected > 0 ? destructive : changes).Add(change);
        }

        return new SchemaDiffPlan(changes, destructive);
    }

    private static bool RefMatches(string reference, Guid id) =>
        Guid.TryParse(reference, out var parsed) && parsed == id;

    private static bool ParametersChanged(Guid columnId, SchemaColumnDraft requested,
        IReadOnlyList<Domain.Schema.AttributeParameterValue> current)
    {
        var old = current.Where(x => x.MetaAttributeId == columnId)
            .ToDictionary(x => x.ParameterDefinitionId, x => x.ParameterValue);
        return requested.Parameters!.Count != old.Count || requested.Parameters.Any(x =>
            !old.TryGetValue(x.ParameterDefinitionId!.Value, out var value) || value != x.Value);
    }

    private static bool ParametersShrink(Guid columnId, SchemaColumnDraft requested,
        IReadOnlyList<Domain.Schema.AttributeParameterValue> current, IEnumerable<string> values)
    {
        var old = current.Where(x => x.MetaAttributeId == columnId)
            .ToDictionary(x => x.ParameterDefinitionId, x => x.ParameterValue);
        return requested.Parameters!.Any(x => old.TryGetValue(x.ParameterDefinitionId!.Value, out var value) &&
            Decimal.TryParse(value, out var oldNumber) && Decimal.TryParse(x.Value, out var newNumber) &&
            newNumber < oldNumber && values.Any(v => v.Length > newNumber));
    }

    private static bool CanConvert(string value, string typeName)
    {
        var type = typeName.ToLowerInvariant();
        if (type.Contains("int"))
        {
            return Int64.TryParse(value, out _);
        }

        if (type.Contains("decimal") || type.Contains("numeric") || type.Contains("real") || type.Contains("double"))
        {
            return Decimal.TryParse(value, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        if (type.Contains("bool"))
        {
            return Boolean.TryParse(value, out _) || value is "0" or "1";
        }

        if (type.Contains("date") || type.Contains("time"))
        {
            return DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _);
        }

        return true;
    }

    private static string TablePath(SchemaTableDraft table) =>
        $"tables[{table.Id?.ToString() ?? table.TempId}]";

    private static SchemaChangeResponse Change(
        string kind, string entityType, Guid? id, string? tempId, string path, string description,
        long? affectedRows = null, string severity = "Info", string? code = null) => new()
        {
            Kind = kind,
            EntityType = entityType,
            EntityId = id,
            TempId = tempId,
            Path = path,
            Description = description,
            AffectedRows = affectedRows,
            Severity = severity,
            Code = code
        };
}
