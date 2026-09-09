using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal interface ISchemaSnapshotReader
{
    Task<TargetDbSchemaResponse?> ReadAsync(Guid targetDbId, CancellationToken ct);
}

internal sealed class SchemaSnapshotReader(AppDbContext db) : ISchemaSnapshotReader
{
    public async Task<TargetDbSchemaResponse?> ReadAsync(Guid targetDbId, CancellationToken ct)
    {
        var targetDb = await db.TargetDbs.AsNoTracking()
            .Where(x => x.Id == targetDbId)
            .Select(x => new
            {
                x.Id,
                x.DbmsId,
                x.DbName,
                x.IsReadOnly,
                x.SchemaVersion
            })
            .SingleOrDefaultAsync(ct);
        if (targetDb is null)
        {
            return null;
        }

        var tables = await db.MetaTables.AsNoTracking()
            .Where(x => x.TargetDbId == targetDbId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.TableName,
                x.Description,
                x.SortOrder
            })
            .ToListAsync(ct);
        var tableIds = tables.Select(x => x.Id).ToList();

        var columns = await db.MetaAttributes.AsNoTracking()
            .Where(x => tableIds.Contains(x.MetaTableId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.MetaTableId,
                x.AttributeName,
                x.PhysicalTypeId,
                PhysicalTypeName = x.PhysicalType.TypeName,
                x.IsPrimaryKey,
                x.IsRequired,
                x.SortOrder
            })
            .ToListAsync(ct);
        var columnIds = columns.Select(x => x.Id).ToList();

        var parameters = await db.AttributeParameterValues.AsNoTracking()
            .Where(x => columnIds.Contains(x.MetaAttributeId))
            .OrderBy(x => x.ParameterDefinition.SortOrder)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.MetaAttributeId,
                x.ParameterDefinitionId,
                x.ParameterDefinition.ParameterKey,
                x.ParameterDefinition.DisplayName,
                x.ParameterDefinition.InputType,
                Value = x.ParameterValue,
                x.ParameterDefinition.IsRequired
            })
            .ToListAsync(ct);
        var parameterLookup = parameters.ToLookup(x => x.MetaAttributeId);
        var columnLookup = columns.ToLookup(x => x.MetaTableId);

        var relationships = await db.MetaRelationships.AsNoTracking()
            .Where(x => columnIds.Contains(x.SourceAttributeId))
            .OrderBy(x => x.RelationshipName)
            .ThenBy(x => x.Id)
            .Select(x => new SchemaRelationshipResponse
            {
                Id = x.Id,
                Name = x.RelationshipName,
                SourceColumnId = x.SourceAttributeId,
                TargetColumnId = x.TargetAttributeId,
                DeleteRule = x.DeleteRule,
                UpdateRule = x.UpdateRule
            })
            .ToListAsync(ct);

        var hasTasks = await db.SqlQueries.AsNoTracking()
            .AnyAsync(x => x.TargetDbId == targetDbId, ct);
        var ready = tables.Count > 0;
        var canEdit = !targetDb.IsReadOnly;

        return new TargetDbSchemaResponse
        {
            TargetDbId = targetDb.Id,
            DbmsId = targetDb.DbmsId,
            DbName = targetDb.DbName,
            Version = targetDb.SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            State = ready ? SchemaLifecycleState.Ready : SchemaLifecycleState.Draft,
            Capabilities = new SchemaCapabilitiesResponse
            {
                CanEditSchema = canEdit,
                CanValidateSchema = canEdit,
                CanApplySchema = canEdit,
                CanEditData = canEdit && ready,
                CanDeleteTargetDb = !hasTasks,
                SchemaEditBlockReason = canEdit ? null : "База доступна только для чтения.",
                DataEditBlockReason = !canEdit
                    ? "База доступна только для чтения."
                    : ready ? null : "Сначала примените схему.",
                DeleteTargetDbBlockReason = hasTasks
                    ? "База используется эталонными решениями заданий."
                    : null
            },
            Tables = tables.Select(table => new SchemaTableResponse
            {
                Id = table.Id,
                Name = table.TableName,
                Description = table.Description,
                SortOrder = table.SortOrder,
                Columns = columnLookup[table.Id].Select(column => new SchemaColumnResponse
                {
                    Id = column.Id,
                    Name = column.AttributeName,
                    PhysicalTypeId = column.PhysicalTypeId,
                    PhysicalTypeName = column.PhysicalTypeName,
                    IsPrimaryKey = column.IsPrimaryKey,
                    IsRequired = column.IsRequired,
                    SortOrder = column.SortOrder,
                    Parameters = parameterLookup[column.Id].Select(parameter => new SchemaParameterResponse
                    {
                        ParameterDefinitionId = parameter.ParameterDefinitionId,
                        ParameterKey = parameter.ParameterKey,
                        DisplayName = parameter.DisplayName,
                        InputType = parameter.InputType,
                        Value = parameter.Value,
                        IsRequired = parameter.IsRequired
                    }).ToList()
                }).ToList()
            }).ToList(),
            Relationships = relationships
        };
    }
}
