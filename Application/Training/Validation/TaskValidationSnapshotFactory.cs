using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Common.Results;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Validation;

/// <summary>Живое состояние схемы, данных и эталона задания в том же каноническом виде, что и в версии оценки.</summary>
public sealed record LiveTaskSnapshot(string SchemaJson, string DatasetJson, string ReferenceQueryJson);

public interface ITaskValidationSnapshotFactory
{
    Task<Result<TaskValidationVersionSnapshot>> CreateAsync(
        Guid taskId,
        TaskValidationConfiguration configuration,
        QueryResultSet referenceResult,
        string analyzerVersion,
        CancellationToken ct);

    Task<Result<LiveTaskSnapshot>> CreateLiveAsync(Guid taskId, CancellationToken ct);
}

public sealed class TaskValidationSnapshotFactory(
    IApplicationDbContext db,
    ValidationSkillResolver skillResolver) : ITaskValidationSnapshotFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<TaskValidationVersionSnapshot>> CreateAsync(
        Guid taskId,
        TaskValidationConfiguration configuration,
        QueryResultSet referenceResult,
        string analyzerVersion,
        CancellationToken ct)
    {
        var assignedSkills = await skillResolver.EnsureAsync(
            TaskValidationMappings.ToDefinition(configuration), ct);
        if (!assignedSkills.IsSuccess)
        {
            return Result<TaskValidationVersionSnapshot>.Fail(assignedSkills.Error!);
        }

        var live = await CreateLiveAsync(taskId, ct);
        if (!live.IsSuccess)
        {
            return Result<TaskValidationVersionSnapshot>.Fail(live.Error!);
        }

        var checkDefinitions = configuration.Checks
            .OrderBy(check => check.Order)
            .Select(check => new
            {
                Check = check,
                Skill = ValidationSkillResolver.Resolve(new ValidationCheckDefinition(
                    check.Id, check.Kind, check.Value, check.Weight, check.Order))
            })
            .ToArray();
        var skills = assignedSkills.Value!;
        var skillsByExternalId = skills.ToDictionary(skill => skill.SkillId, StringComparer.Ordinal);
        var skillsById = skills.ToDictionary(skill => skill.Id);

        var configurationSnapshot = new
        {
            configuration.PassingScore,
            VisibleHintGroups = configuration.GetVisibleHintGroups(),
            Checks = checkDefinitions.Select(item => new
            {
                item.Check.Id,
                item.Check.Kind,
                item.Check.Value,
                item.Check.Weight,
                item.Check.Order,
                Skill = ToSnapshot(skillsByExternalId[item.Skill.SkillId])
            }),
            SkillTags = configuration.SkillTags.Select(tag => ToSnapshot(skillsById[tag.SkillId]))
        };

        return new TaskValidationVersionSnapshot(
            live.Value!.SchemaJson,
            live.Value.DatasetJson,
            live.Value.ReferenceQueryJson,
            GoldenResult.Serialize(referenceResult),
            JsonSerializer.Serialize(configurationSnapshot, JsonOptions),
            analyzerVersion);
    }

    private static object ToSnapshot(Skill skill) => new
    {
        skill.Id,
        skill.SkillId,
        skill.Name,
        skill.Description,
        skill.Origin
    };

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
