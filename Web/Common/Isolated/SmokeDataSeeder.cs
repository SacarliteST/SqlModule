using System.Data;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Common;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.Validation;

namespace SQLModule.Web.Common.Isolated;

/// <summary>Создаёт стабильное Published-задание для сквозного platform smoke-теста.</summary>
internal sealed class SmokeDataSeeder(
    AppDbContext db,
    ITaskValidationEvaluationService evaluationService,
    ITaskValidationSnapshotFactory snapshotFactory,
    TimeProvider timeProvider)
{
    internal static readonly Guid DbmsId = new("10000000-0000-0000-0000-000000000002");
    internal static readonly Guid IntegerTypeId = new("20000000-0000-0000-0000-000000000007");
    internal static readonly Guid TargetDbId = new("30000000-0000-0000-0000-000000000002");
    internal static readonly Guid TableId = new("40000000-0000-0000-0000-000000000002");
    internal static readonly Guid AttributeId = new("50000000-0000-0000-0000-000000000002");
    internal static readonly Guid RecordId = new("51000000-0000-0000-0000-000000000002");
    internal static readonly Guid CellId = new("52000000-0000-0000-0000-000000000002");
    internal static readonly Guid TopicId = new("60000000-0000-0000-0000-000000000002");
    internal static readonly Guid QueryId = new("70000000-0000-0000-0000-000000000002");
    internal static readonly Guid TaskId = new("80000000-0000-0000-0000-000000000002");
    internal static readonly Guid ValidationConfigurationId = new("90000000-0000-0000-0000-000000000002");
    internal static readonly Guid ValidationCheckId = new("91000000-0000-0000-0000-000000000002");

    internal const string TaskName = "Smoke: выбрать идентификаторы пользователей";
    internal const string TaskText = "Получите идентификаторы всех пользователей из таблицы users.";
    internal const string ReferenceSql = "SELECT id FROM users ORDER BY id;";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var dbms = await db.DbmsDictionaries.SingleOrDefaultAsync(value => value.Id == DbmsId, ct);
        var integerType = await db.PhysicalTypes.SingleOrDefaultAsync(value => value.Id == IntegerTypeId, ct);
        var targetDb = await db.TargetDbs.SingleOrDefaultAsync(value => value.Id == TargetDbId, ct);
        var table = await db.MetaTables.SingleOrDefaultAsync(value => value.Id == TableId, ct);
        var attribute = await db.MetaAttributes.SingleOrDefaultAsync(value => value.Id == AttributeId, ct);
        var record = await db.DataRecords.SingleOrDefaultAsync(value => value.Id == RecordId, ct);
        var cell = await db.CellValues.SingleOrDefaultAsync(value => value.Id == CellId, ct);
        var topic = await db.Topics.SingleOrDefaultAsync(value => value.Id == TopicId, ct);
        var query = await db.SqlQueries.SingleOrDefaultAsync(value => value.Id == QueryId, ct);
        var task = await db.SqlTasks.SingleOrDefaultAsync(value => value.Id == TaskId, ct);

        ValidateOwnership(integerType, targetDb, table, attribute, record, cell, topic, query, task);

        dbms ??= Add(DbmsDictionary.Create(
            "PostgreSQL (Smoke)",
            "postgres",
            "postgres:16-alpine",
            5432,
            "POSTGRES_USER",
            "POSTGRES_PASSWORD",
            "POSTGRES_DB",
            null,
            "training",
            "smoke",
            "smoke",
            DbmsId));
        dbms.Update(
            "PostgreSQL (Smoke)", "postgres", "postgres:16-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "smoke", "smoke");

        integerType ??= Add(PhysicalType.Create(DbmsId, "integer", IntegerTypeId));
        integerType.Update("integer");

        targetDb ??= Add(TargetDb.Create(
            DbmsId, "Smoke training DB", "Минимальная база platform smoke-теста", false, TargetDbId));
        targetDb.Update("Smoke training DB", "Минимальная база platform smoke-теста", false);

        table ??= Add(MetaTable.Create(TargetDbId, "users", "Пользователи", TableId, 0));
        table.Update("users", "Пользователи", 0);

        attribute ??= Add(MetaAttribute.Create(
            TableId, IntegerTypeId, "id", true, true, 0, AttributeId));
        attribute.UpdateSchema("id", IntegerTypeId, true, true, 0);

        record ??= Add(DataRecord.Create(TableId, 0, RecordId));
        record.Update(0);

        cell ??= Add(CellValue.Create(RecordId, AttributeId, "1", CellId));
        cell.Update("1");

        topic ??= Add(Topic.Create("Platform smoke", null, TopicId, "Стабильные задания smoke-теста"));
        topic.Update("Platform smoke", "Стабильные задания smoke-теста");

        query ??= Add(SqlQuery.Create(ReferenceSql, false, false, TargetDbId, QueryId));
        query.Update(TargetDbId, ReferenceSql, false, false);
        query.SetExpectedResult(GoldenResult.Serialize(
            new QueryResultSet(true, null, ["id"], [["1"]], 1, 1)));

        task ??= Add(SqlTask.Create(
            TopicId, QueryId, TaskName, TaskText, 1, TaskId, PublicationStatus.Published));
        task.Update(TaskName, TaskText, 1, PublicationStatus.Published);

        await db.SaveChangesAsync(ct);

        if (!task.ActiveValidationVersionId.HasValue)
        {
            // Platform-flow (PlatformProgressService.EnsureCreatedAsync) с Phase 2b требует
            // опубликованную конфигурацию проверки для любой platform-сессии — без этого
            // блока задание смоук-теста существовало бы только в "легаси"-режиме и
            // platform-сабмит отвечал бы 422 ModuleSession.TaskValidationUnavailable.
            await PublishValidationAsync(task, ct);
        }

        await transaction.CommitAsync(ct);
    }

    private async Task PublishValidationAsync(SqlTask task, CancellationToken ct)
    {
        var configuration = await db.TaskValidationConfigurations
            .Include(value => value.Checks)
            .SingleOrDefaultAsync(value => value.TaskId == TaskId, ct);
        if (configuration is null)
        {
            configuration = TaskValidationConfiguration.Create(
                TaskId, 100, null, [HintGroup.Result], ValidationConfigurationId);
            db.Add(configuration);
        }
        else
        {
            // Черновик мог остаться от ручного редактирования (например, открыли редактор проверки).
            configuration.UpdateSettings(100, null, [HintGroup.Result]);
        }

        var existingCheckIds = configuration.Checks.Select(check => check.Id).ToHashSet();
        configuration.SynchronizeChecks([
            new ValidationCheckDefinition(ValidationCheckId, ValidationCheckKind.MainDatasetResult, null, 100, 0)
        ]);
        // Новый критерий с заранее заданным Id EF без явного Add трекает как Modified и падает
        // с DbUpdateConcurrencyException на существующей конфигурации — так же делает UpdateTaskValidationHandler.
        foreach (var addedCheck in configuration.Checks.Where(check => !existingCheckIds.Contains(check.Id)))
        {
            db.ValidationChecks.Add(addedCheck);
        }

        await db.SaveChangesAsync(ct);

        var definition = TaskValidationMappings.ToDefinition(configuration);
        var identities = TaskValidationMappings.ToIdentities(definition);
        var evaluation = await evaluationService.EvaluateAsync(TaskId, definition, identities, ct);
        if (!evaluation.IsSuccess || !evaluation.Value!.Response.IsValid || evaluation.Value.ReferenceResult is null)
        {
            throw new InvalidOperationException(
                $"Smoke seed: эталонное решение задания {TaskId:D} не прошло проверку конфигурации " +
                $"({evaluation.Error?.Message ?? evaluation.Value?.Response.Violations.FirstOrDefault()?.Message}).");
        }

        var snapshot = await snapshotFactory.CreateAsync(
            TaskId, configuration, evaluation.Value.ReferenceResult, evaluation.Value.Response.AnalyzerVersion, ct);
        if (!snapshot.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Smoke seed: не удалось построить snapshot проверки для задания {TaskId:D} ({snapshot.Error!.Message}).");
        }

        var version = TaskValidationVersion.Publish(
            TaskId, 1, configuration.Version, configuration.PassingScore, configuration.MaxAttempts,
            configuration.VisibleHintGroupsMask, snapshot.Value!,
            SystemUser.Id, "Smoke seed", timeProvider.GetUtcNow());
        db.Add(version);
        task.ActivateValidationVersion(version.Id);
        await db.SaveChangesAsync(ct);
    }

    private T Add<T>(T entity) where T : class
    {
        db.Add(entity);
        return entity;
    }

    private static void ValidateOwnership(
        PhysicalType? integerType,
        TargetDb? targetDb,
        MetaTable? table,
        MetaAttribute? attribute,
        DataRecord? record,
        CellValue? cell,
        Topic? topic,
        SqlQuery? query,
        SqlTask? task)
    {
        RequireOwner(integerType?.DbmsId, DbmsId, nameof(PhysicalType), IntegerTypeId);
        RequireOwner(targetDb?.DbmsId, DbmsId, nameof(TargetDb), TargetDbId);
        RequireOwner(table?.TargetDbId, TargetDbId, nameof(MetaTable), TableId);
        RequireOwner(attribute?.MetaTableId, TableId, nameof(MetaAttribute), AttributeId);
        RequireOwner(attribute?.PhysicalTypeId, IntegerTypeId, nameof(MetaAttribute), AttributeId);
        RequireOwner(record?.MetaTableId, TableId, nameof(DataRecord), RecordId);
        RequireOwner(cell?.DataRecordId, RecordId, nameof(CellValue), CellId);
        RequireOwner(cell?.MetaAttributeId, AttributeId, nameof(CellValue), CellId);
        RequireOwner(topic?.ParentTopicId, null, nameof(Topic), TopicId);
        RequireOwner(query?.TargetDbId, TargetDbId, nameof(SqlQuery), QueryId);
        RequireOwner(task?.TopicId, TopicId, nameof(SqlTask), TaskId);
        RequireOwner(task?.SqlQueryId, QueryId, nameof(SqlTask), TaskId);
    }

    private static void RequireOwner(Guid? actual, Guid? expected, string entityType, Guid entityId)
    {
        if (actual.HasValue && actual != expected)
        {
            throw new InvalidOperationException(
                $"Smoke seed не может использовать {entityType} {entityId:D}: запись принадлежит другому набору данных.");
        }
    }
}
