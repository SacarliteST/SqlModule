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
public sealed class ModuleSessionLimitsMigrationTests(TestApplication app)
{
    private const string PreviousMigration = "20261009112457_AddOutboxLease";

    [Fact(DisplayName = "Migration переносит progress в сессию и перенумеровывает старые попытки")]
    public async Task Migration_MovesLegacySessionStateAndRenumbersAttempts()
    {
        var databaseName = $"limits_{Guid.NewGuid():N}";
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
            await using (var db = new PostgreSqlDbContext(options))
            {
                var migrator = db.Database.GetService<IMigrator>();
                await migrator.MigrateAsync(PreviousMigration);
                await SeedLegacyDataAsync(db);
                await migrator.MigrateAsync();
            }

            await using var assertionDb = new PostgreSqlDbContext(options);
            var sessions = await assertionDb.ModuleSessions.AsNoTracking()
                .OrderBy(value => value.SessionKey)
                .ToArrayAsync();
            sessions.Length.ShouldBe(2);
            sessions[0].MaxAttempts.ShouldBe(3);
            sessions[0].ValidationVersionId.ShouldNotBeNull();
            sessions[0].FinalScore.ShouldBeNull();
            sessions[1].MaxAttempts.ShouldBe(3);
            sessions[1].FinalScore.ShouldBe(80);
            sessions[1].FinalizationReason.ShouldBe(FinalizationReason.Manual);

            var standaloneNumbers = await assertionDb.Attempts.AsNoTracking()
                .Where(value => value.ModuleSessionId == null)
                .OrderBy(value => value.AttemptNumber)
                .Select(value => value.AttemptNumber)
                .ToArrayAsync();
            standaloneNumbers.ShouldBe([1, 2]);
            var platformNumbers = await assertionDb.Attempts.AsNoTracking()
                .Where(value => value.ModuleSessionId != null)
                .Select(value => value.AttemptNumber)
                .ToArrayAsync();
            platformNumbers.ShouldBe([1, 1]);
        }
        finally
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task SeedLegacyDataAsync(PostgreSqlDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var dbms = DbmsDictionary.Create(
            "Migration PostgreSQL", "postgres", "postgres:16-alpine", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password");
        var targetDb = TargetDb.Create(dbms.Id, "migration_training", null, false);
        var topic = Topic.Create("Migration topic");
        var query = SqlQuery.Create("SELECT 1", false, false, targetDb.Id);
        query.SetExpectedResult("{\"columns\":[],\"rows\":[]}");
        var task = SqlTask.Create(
            topic.Id, query.Id, "Migration task", "Migration task", 1,
            publicationStatus: PublicationStatus.Published);
        db.AddRange(dbms, targetDb, topic, query, task);
        await db.SaveChangesAsync();

        var version = TaskValidationVersion.Publish(
            task.Id,
            1,
            Guid.NewGuid(),
            60,
            3,
            0,
            new TaskValidationVersionSnapshot("{}", "{}", "{}", "{}", "{}", "1"),
            Guid.NewGuid(),
            "migration-test",
            now);
        db.TaskValidationVersions.Add(version);
        task.ActivateValidationVersion(version.Id);
        await db.SaveChangesAsync();

        var activeSessionId = Guid.NewGuid();
        var completedSessionId = Guid.NewGuid();
        await InsertLegacySessionAsync(db, activeSessionId, task.Id, "1-active", "ACTIVE", now);
        await InsertLegacySessionAsync(db, completedSessionId, task.Id, "2-completed", "COMPLETED", now);

        var activeProgress = StudentTaskProgress.CreatePlatform(
            Guid.NewGuid(), task.Id, version.Id, activeSessionId, null);
        activeProgress.RecordCountedAttempt(40);
        var completedProgress = StudentTaskProgress.CreatePlatform(
            Guid.NewGuid(), task.Id, version.Id, completedSessionId, null);
        completedProgress.RecordCountedAttempt(80);
        completedProgress.BeginFinalization(FinalizationReason.Manual, now);
        completedProgress.MarkCompletionPending();
        completedProgress.MarkCompleted();
        var standaloneUser = Guid.NewGuid();
        var restartedProgress = StudentTaskProgress.CreateStandalone(standaloneUser, task.Id, version.Id);
        restartedProgress.RecordCountedAttempt(10);
        restartedProgress.CloseForRestart(now);
        var currentProgress = StudentTaskProgress.CreateStandalone(standaloneUser, task.Id, version.Id);
        currentProgress.RecordCountedAttempt(20);
        db.StudentTaskProgresses.AddRange(
            activeProgress, completedProgress, restartedProgress, currentProgress);
        await db.SaveChangesAsync();

        var platformAttempt = CreateAttempt(activeProgress.UserId, task.Id, activeSessionId, now, "platform");
        platformAttempt.AttachScoring(activeProgress.Id, version.Id, 1, 40, true);
        var restartedAttempt = CreateAttempt(standaloneUser, task.Id, null, now.AddMinutes(-2), "old");
        restartedAttempt.AttachScoring(restartedProgress.Id, version.Id, 1, 10, true);
        var currentAttempt = CreateAttempt(standaloneUser, task.Id, null, now.AddMinutes(-1), "new");
        currentAttempt.AttachScoring(currentProgress.Id, version.Id, 1, 20, true);
        // Один progress, две сессии: в старой схеме номера идут сквозные (1, 2), после миграции — по 1 в каждой сессии.
        var secondSessionAttempt = CreateAttempt(activeProgress.UserId, task.Id, completedSessionId, now.AddMinutes(1), "platform-2");
        secondSessionAttempt.AttachScoring(activeProgress.Id, version.Id, 2, 50, true);
        db.Attempts.AddRange(platformAttempt, secondSessionAttempt, restartedAttempt, currentAttempt);
        await db.SaveChangesAsync();
    }

    private static Attempt CreateAttempt(
        Guid userId,
        Guid taskId,
        Guid? sessionId,
        DateTimeOffset at,
        string sql) => Attempt.Record(
        userId,
        taskId,
        sql,
        ExecutionStatus.Succeeded,
        false,
        CheckReason.ValueMismatch,
        0,
        1,
        null,
        at,
        at,
        moduleSessionId: sessionId);

    private static Task InsertLegacySessionAsync(
        PostgreSqlDbContext db,
        Guid sessionId,
        Guid taskId,
        string sessionKey,
        string status,
        DateTimeOffset now) => db.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO "ModuleSessions"
            ("SessionId", "SessionKey", "UserId", "TaskRef", "ReturnUrl", "ExpiresAt", "Status",
             "CreatedById", "CreatedByName", "CreatedAt", "UpdatedById", "UpdatedByName", "UpdatedAt")
        VALUES
            ({sessionId}, {sessionKey}, {Guid.NewGuid()}, {taskId.ToString()}, {"https://platform.example/return"},
             {(DateTimeOffset?)null}, {status}, {Guid.Empty}, {"migration-test"}, {now}, {Guid.Empty}, {"migration-test"}, {now})
        """);
}
