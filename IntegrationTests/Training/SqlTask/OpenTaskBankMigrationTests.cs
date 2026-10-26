using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Npgsql;
using Shouldly;
using SQLModule.Data.Core.Configurations;
using SQLModule.Data.Core.Migrations.PostgreSql;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.Infrastructure;

namespace SQLModule.IntegrationTests.Training.SqlTask;

/// <summary>Проверяет миграцию данных открытого банка заданий.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OpenTaskBankMigrationTests
{
    private const string PreviousMigration = "20261025173157_RemoveLegacyProgress";
    private readonly TestApplication app;

    public OpenTaskBankMigrationTests(TestApplication app)
    {
        this.app = app;
    }

    [Fact(DisplayName = "Миграция банка сохраняет задания закрытыми и создаёт инфраструктуру нумерации")]
    public async Task Migration_UsesControlDefaultsAndCreatesBankInfrastructure()
    {
        var databaseName = $"task_bank_{Guid.NewGuid():N}";
        var baseBuilder = new NpgsqlConnectionStringBuilder(app.ConnectionString);
        var adminBuilder = new NpgsqlConnectionStringBuilder(app.ConnectionString) { Database = "postgres" };
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
        {
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var databaseBuilder = new NpgsqlConnectionStringBuilder(baseBuilder.ConnectionString)
            {
                Database = databaseName
            };
            var options = Options.Create(new ConnectionOptions
            {
                ConnectionString = databaseBuilder.ConnectionString
            });
            Guid taskId;
            await using (var db = new PostgreSqlDbContext(options))
            {
                var migrator = db.Database.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigration);
                taskId = await SeedExistingTaskAsync(db);
                await migrator.MigrateAsync();
            }

            await using var assertionDb = new PostgreSqlDbContext(options);
            var task = await assertionDb.SqlTasks.AsNoTracking().SingleAsync(value => value.Id == taskId);
            task.Visibility.ShouldBe(TaskVisibility.Control);
            task.BankNumber.ShouldBeNull();
            task.BankPublishedAt.ShouldBeNull();
            task.ShowSolution.ShouldBeTrue();

            var infrastructure = await assertionDb.Database.SqlQueryRaw<string>(
                """
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND table_name IN ('BankHintAccesses', 'BankSolutionAccesses')
                UNION ALL
                SELECT sequence_name AS "Value"
                FROM information_schema.sequences
                WHERE sequence_schema = 'public'
                  AND sequence_name = 'SqlTaskBankNumberSequence'
                """).ToListAsync();
            infrastructure.OrderBy(value => value).ShouldBe([
                "BankHintAccesses",
                "BankSolutionAccesses",
                "SqlTaskBankNumberSequence"
            ]);
        }
        finally
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<Guid> SeedExistingTaskAsync(PostgreSqlDbContext db)
    {
        var dbms = DbmsDictionary.Create(
            "Bank migration PostgreSQL", "postgres", "postgres:16-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var targetDb = TargetDb.Create(dbms.Id, "bank_migration", null, false);
        var topic = Domain.Training.Topic.Create("Bank migration topic");
        var query = Domain.Training.SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        db.AddRange(dbms, targetDb, topic, query);
        await db.SaveChangesAsync();

        var taskId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SqlTasks"
                ("Id", "TopicId", "SqlQueryId", "TaskName", "TaskText", "DifficultyLevel",
                 "PublicationStatus", "ActiveValidationVersionId",
                 "CreatedById", "CreatedByName", "CreatedAt", "UpdatedById", "UpdatedByName", "UpdatedAt")
            VALUES
                ({taskId}, {topic.Id}, {query.Id}, {"Существующее задание"}, {"Условие"}, {(short)1},
                 {"Published"}, {(Guid?)null},
                 {actorId}, {"migration-test"}, {now}, {actorId}, {"migration-test"}, {now})
            """);
        return taskId;
    }
}
