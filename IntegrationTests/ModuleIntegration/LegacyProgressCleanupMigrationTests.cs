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

namespace SQLModule.IntegrationTests.ModuleIntegration;

[Collection(IntegrationTestCollection.Name)]
public sealed class LegacyProgressCleanupMigrationTests
{
    private readonly TestApplication app;

    public LegacyProgressCleanupMigrationTests(TestApplication app)
    {
        this.app = app;
    }

    private const string PreviousMigration = "20261023203434_MoveAttemptLimitsToModuleSession";

    [Fact(DisplayName = "Migration удаляет legacy progress и сохраняет попытки")]
    public async Task Migration_RemovesLegacyProgressAndKeepsAttempts()
    {
        var databaseName = $"cleanup_{Guid.NewGuid():N}";
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
            Guid attemptId;
            await using (var db = new PostgreSqlDbContext(options))
            {
                var migrator = db.Database.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigration);
                attemptId = await SeedLegacyDataAsync(db);
                await migrator.MigrateAsync();
            }

            await using var assertionDb = new PostgreSqlDbContext(options);
            var attempt = await assertionDb.Attempts.AsNoTracking().SingleAsync(value => value.Id == attemptId);
            attempt.AttemptNumber.ShouldBe(1);
            attempt.Score.ShouldBe(80);

            var legacyObjects = await assertionDb.Database.SqlQueryRaw<string>(
                """
                SELECT "table_name" AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND "table_name" IN ('StudentTaskProgresses', 'AttemptReservations')
                UNION ALL
                SELECT "column_name" AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND (("table_name" = 'Attempts' AND "column_name" = 'ProgressId')
                    OR ("table_name" IN ('TaskValidationConfigurations', 'TaskValidationVersions')
                      AND "column_name" = 'MaxAttempts'))
                """).ToListAsync();
            legacyObjects.ShouldBeEmpty();
        }
        finally
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<Guid> SeedLegacyDataAsync(PostgreSqlDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var dbms = DbmsDictionary.Create(
            "Cleanup PostgreSQL", "postgres", "postgres:16-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var targetDb = TargetDb.Create(dbms.Id, "cleanup_training", null, false);
        var topic = Topic.Create("Cleanup topic");
        var query = SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        query.SetExpectedResult("{\"columns\":[],\"rows\":[]}");
        var task = SqlTask.Create(
            topic.Id, query.Id, "Cleanup task", "Cleanup task", 1,
            publicationStatus: PublicationStatus.Published);
        db.AddRange(dbms, targetDb, topic, query, task);
        await db.SaveChangesAsync();

        var version = TaskValidationVersion.Publish(
            task.Id,
            1,
            Guid.NewGuid(),
            60,
            0,
            new TaskValidationVersionSnapshot("{}", "{}", "{}", "{}", "{}", "1"),
            Guid.NewGuid(),
            "migration-test",
            now);
        db.TaskValidationVersions.Add(version);
        task.ActivateValidationVersion(version.Id);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var attempt = Attempt.Record(
            userId, task.Id, "SELECT 1", ExecutionStatus.Succeeded, false,
            CheckReason.ValueMismatch, 0, 1, null, now, now);
        attempt.AttachScoring(version.Id, 1, 80, true);
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync();

        var progressId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "StudentTaskProgresses"
                ("Id", "UserId", "TaskId", "ModuleSessionId", "ValidationVersionId",
                 "AttemptsUsed", "NextAttemptNumber", "BestScore", "Status", "ConcurrencyVersion",
                 "CreatedById", "CreatedByName", "CreatedAt", "UpdatedById", "UpdatedByName", "UpdatedAt")
            VALUES
                ({progressId}, {userId}, {task.Id}, {(Guid?)null}, {version.Id},
                 {1}, {2}, {80}, {"Active"}, {Guid.NewGuid()},
                 {userId}, {"migration-test"}, {now}, {userId}, {"migration-test"}, {now})
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "Attempts" SET "ProgressId" = {progressId} WHERE "Id" = {attempt.Id}
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AttemptReservations"
                ("Id", "ProgressId", "AttemptNumber", "IdempotencyKey", "PayloadHash", "State",
                 "AttemptId", "CreatedAt", "UpdatedAt")
            VALUES
                ({Guid.NewGuid()}, {progressId}, {1}, {Guid.NewGuid()}, {new string('A', 64)}, {"Completed"},
                 {attempt.Id}, {now}, {now})
            """);
        return attempt.Id;
    }
}
