using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Common.Isolated;

/// <summary>
/// Засевает минимальный демо-набор данных при старте в изолированном режиме (идемпотентно).
/// </summary>
internal sealed class DemoDataSeeder(AppDbContext db)
{
    private static readonly Guid DbmsId = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid IntegerTypeId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid TextTypeId = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid VarcharTypeId = new("20000000-0000-0000-0000-000000000003");
    private static readonly Guid BooleanTypeId = new("20000000-0000-0000-0000-000000000004");
    private static readonly Guid DateTypeId = new("20000000-0000-0000-0000-000000000005");
    private static readonly Guid NumericTypeId = new("20000000-0000-0000-0000-000000000006");
    private static readonly Guid VarcharLengthId = new("21000000-0000-0000-0000-000000000001");
    private static readonly Guid NumericPrecisionId = new("21000000-0000-0000-0000-000000000002");
    private static readonly Guid NumericScaleId = new("21000000-0000-0000-0000-000000000003");
    private static readonly Guid TargetDbId = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid MetaTableId = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid MetaAttributeId = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid TopicId = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid SqlQueryId = new("70000000-0000-0000-0000-000000000001");
    private static readonly Guid SqlTaskId = new("80000000-0000-0000-0000-000000000001");

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var dbmsExists = await db.DbmsDictionaries.AnyAsync(d => d.Id == DbmsId, ct);
        if (!dbmsExists)
        {
            var dbms = DbmsDictionary.Create(
                dbmsName: "PostgreSQL (Demo)",
                dbmsSystemName: "postgres",
                dockerImage: "postgres:latest",
                defaultPort: 5432,
                envUserKey: "POSTGRES_USER",
                envPasswordKey: "POSTGRES_PASSWORD",
                envDatabaseKey: "POSTGRES_DB",
                extraEnvConfig: null,
                defaultDatabase: "demo",
                defaultUsername: "demo",
                defaultPassword: "demo",
                id: DbmsId);
            db.DbmsDictionaries.Add(dbms);
        }

        await EnsurePhysicalTypesAsync(ct);

        if (dbmsExists)
        {
            await db.SaveChangesAsync(ct);
            return;
        }

        var targetDb = TargetDb.Create(DbmsId, "Demo DB", "Демонстрационная база данных", false, TargetDbId);
        db.TargetDbs.Add(targetDb);

        var metaTable = MetaTable.Create(TargetDbId, "users", "Пользователи (demo)", MetaTableId);
        db.MetaTables.Add(metaTable);

        var metaAttr = MetaAttribute.Create(MetaTableId, IntegerTypeId, "id", true, true, 0, MetaAttributeId);
        db.MetaAttributes.Add(metaAttr);

        var topic = Topic.Create("Введение в SQL", null, TopicId);
        db.Topics.Add(topic);

        var fakeResult = new QueryResultSet(true, null, ["id"], [["1"]], 1, 1);
        var sqlQuery = SqlQuery.Create("SELECT id FROM users", false, false, TargetDbId, SqlQueryId);
        sqlQuery.SetExpectedResult(GoldenResult.Serialize(fakeResult));
        db.SqlQueries.Add(sqlQuery);

        var sqlTask = SqlTask.Create(TopicId, SqlQueryId, "Выбрать id из users", "Напишите SELECT для получения id всех пользователей.", 1, SqlTaskId);
        db.SqlTasks.Add(sqlTask);

        await db.SaveChangesAsync(ct);
    }

    private async Task EnsurePhysicalTypesAsync(CancellationToken ct)
    {
        var existingTypes = (await db.PhysicalTypes
            .Where(type => type.DbmsId == DbmsId)
            .Select(type => new { type.Id, type.TypeName })
            .ToListAsync(ct))
            .ToDictionary(type => type.TypeName, type => type.Id, StringComparer.OrdinalIgnoreCase);

        EnsureType(IntegerTypeId, "integer");
        EnsureType(TextTypeId, "text");
        var varcharTypeId = EnsureType(VarcharTypeId, "character varying");
        EnsureType(BooleanTypeId, "boolean");
        EnsureType(DateTypeId, "date");
        var numericTypeId = EnsureType(NumericTypeId, "numeric");

        var parameterTypeIds = new[] { varcharTypeId, numericTypeId };
        var existingParameterKeys = (await db.ParameterDefinitions
            .Where(parameter => parameterTypeIds.Contains(parameter.PhysicalTypeId))
            .Select(parameter => new { parameter.PhysicalTypeId, parameter.ParameterKey })
            .ToListAsync(ct))
            .Select(parameter => (parameter.PhysicalTypeId, parameter.ParameterKey))
            .ToHashSet();

        AddParameter(VarcharLengthId, varcharTypeId, "length", "Длина", "number", "255", 0, "({value})");
        AddParameter(NumericPrecisionId, numericTypeId, "precision", "Точность", "number", "18", 0, "({value}");
        AddParameter(NumericScaleId, numericTypeId, "scale", "Масштаб", "number", "2", 1, ", {value})");

        Guid EnsureType(Guid id, string name)
        {
            if (existingTypes.TryGetValue(name, out var existingId))
            {
                return existingId;
            }

            db.PhysicalTypes.Add(PhysicalType.Create(DbmsId, name, id));
            existingTypes.Add(name, id);
            return id;
        }

        void AddParameter(
            Guid id, Guid physicalTypeId, string key, string displayName,
            string inputType, string defaultValue, short sortOrder, string sqlFragment)
        {
            if (existingParameterKeys.Add((physicalTypeId, key)))
            {
                db.ParameterDefinitions.Add(ParameterDefinition.Create(
                    physicalTypeId, key, displayName, inputType, defaultValue,
                    sortOrder, sqlFragment, true, null, null, null, id));
            }
        }
    }
}
