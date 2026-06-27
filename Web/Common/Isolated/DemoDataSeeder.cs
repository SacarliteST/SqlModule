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
    private static readonly Guid PhysicalTypeId = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid TargetDbId = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid MetaTableId = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid MetaAttributeId = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid TopicId = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid SqlQueryId = new("70000000-0000-0000-0000-000000000001");
    private static readonly Guid SqlTaskId = new("80000000-0000-0000-0000-000000000001");

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.DbmsDictionaries.AnyAsync(d => d.Id == DbmsId, ct))
        {
            return;
        }

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

        var physicalType = PhysicalType.Create(DbmsId, "integer", PhysicalTypeId);
        db.PhysicalTypes.Add(physicalType);

        var targetDb = TargetDb.Create(DbmsId, "Demo DB", "Демонстрационная база данных", false, TargetDbId);
        db.TargetDbs.Add(targetDb);

        var metaTable = MetaTable.Create(TargetDbId, "users", "Пользователи (demo)", MetaTableId);
        db.MetaTables.Add(metaTable);

        var metaAttr = MetaAttribute.Create(MetaTableId, PhysicalTypeId, "id", true, true, 0, MetaAttributeId);
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
}
