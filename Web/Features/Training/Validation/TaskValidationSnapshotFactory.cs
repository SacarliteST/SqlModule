using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.Validation;

/// <summary>Живое состояние схемы, данных и эталона задания в том же каноническом виде, что и в версии оценки.</summary>
internal sealed record LiveTaskSnapshot(string SchemaJson, string DatasetJson, string ReferenceQueryJson);

internal interface ITaskValidationSnapshotFactory
{
    Task<Result<TaskValidationVersionSnapshot>> CreateAsync(
        Guid taskId,
        TaskValidationConfiguration configuration,
        QueryResultSet referenceResult,
        string analyzerVersion,
        CancellationToken ct);

    Task<Result<LiveTaskSnapshot>> CreateLiveAsync(Guid taskId, CancellationToken ct);
}

internal sealed class TaskValidationSnapshotFactory(AppDbContext db) : ITaskValidationSnapshotFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<TaskValidationVersionSnapshot>> CreateAsync(
        Guid taskId,
        TaskValidationConfiguration configuration,
        QueryResultSet referenceResult,
        string analyzerVersion,
        CancellationToken ct)
    {
        var live = await CreateLiveAsync(taskId, ct);
        if (!live.IsSuccess)
        {
            return Result<TaskValidationVersionSnapshot>.Fail(live.Error!);
        }

        var configurationSnapshot = new
        {
            configuration.PassingScore,
            configuration.MaxAttempts,
            VisibleHintGroups = configuration.GetVisibleHintGroups(),
            Checks = configuration.Checks.OrderBy(check => check.Order).Select(check => new
            {
                check.Id,
                check.Kind,
                check.Value,
                check.Weight,
                check.Order
            })
        };

        return new TaskValidationVersionSnapshot(
            live.Value!.SchemaJson,
            live.Value.DatasetJson,
            live.Value.ReferenceQueryJson,
            GoldenResult.Serialize(referenceResult),
            JsonSerializer.Serialize(configurationSnapshot, JsonOptions),
            analyzerVersion);
    }

    public async Task<Result<LiveTaskSnapshot>> CreateLiveAsync(Guid taskId, CancellationToken ct)
    {
        var reference = await db.SqlTasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => new
            {
                task.SqlQuery.TargetDbId,
                task.SqlQuery.QueryText,
                task.SqlQuery.StrictColumnOrder,
                task.SqlQuery.StrictRowOrder
            })
            .SingleAsync(ct);
        var targetDb = await db.TargetDbs
            .AsNoTracking()
            .Include(value => value.Dbms)
            .Include(value => value.MetaTables)
                .ThenInclude(table => table.Attributes)
                    .ThenInclude(attribute => attribute.PhysicalType)
                        .ThenInclude(type => type.ParameterDefinitions)
            .Include(value => value.MetaTables)
                .ThenInclude(table => table.DataRecords)
                    .ThenInclude(record => record.CellValues)
            .SingleOrDefaultAsync(value => value.Id == reference.TargetDbId, ct);
        if (targetDb is null)
        {
            return Result<LiveTaskSnapshot>.Fail(
                TaskValidationErrors.ReferenceInvalid(
                    "Validation.TargetDbNotFound",
                    "Учебная база эталонного запроса недоступна.",
                    "referenceQuery.targetDbId"));
        }

        var tables = targetDb.MetaTables.OrderBy(table => table.SortOrder).ThenBy(table => table.Id).ToArray();
        var attributeIds = tables.SelectMany(table => table.Attributes).Select(attribute => attribute.Id).ToArray();
        var relationships = await db.MetaRelationships
            .AsNoTracking()
            .Where(relationship => attributeIds.Contains(relationship.SourceAttributeId))
            .OrderBy(relationship => relationship.Id)
            .ToArrayAsync(ct);
        var parameterValues = await db.AttributeParameterValues
            .AsNoTracking()
            .Where(value => attributeIds.Contains(value.MetaAttributeId))
            .ToArrayAsync(ct);
        var schema = MetaSchemaMapper.ToSchemaSpec(tables, relationships, parameterValues.ToLookup(
            value => value.MetaAttributeId));
        var dataset = MetaSchemaMapper.ToDataSpec(tables);
        var referenceSnapshot = new
        {
            reference.TargetDbId,
            SqlText = reference.QueryText,
            IsRequiredColumnOrder = reference.StrictColumnOrder,
            IsRequiredRowOrder = reference.StrictRowOrder
        };

        return new LiveTaskSnapshot(
            JsonSerializer.Serialize(schema, JsonOptions),
            JsonSerializer.Serialize(dataset, JsonOptions),
            JsonSerializer.Serialize(referenceSnapshot, JsonOptions));
    }
}
