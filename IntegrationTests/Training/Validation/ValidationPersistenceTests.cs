using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Data.Core;
using SQLModule.IntegrationTests.Infrastructure;

namespace SQLModule.IntegrationTests.Training.Validation;

[Collection(IntegrationTestCollection.Name)]
public sealed class ValidationPersistenceTests(TestApplication app)
{
    [Fact(DisplayName = "Migration создаёт таблицы и ключевые ограничения версионированной проверки")]
    public async Task Migration_CreatesValidationSchema()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT "table_name" AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND "table_name" IN (
                    'TaskValidationConfigurations',
                    'ValidationChecks',
                    'TaskValidationVersions',
                    'AttemptCheckResults')
                """)
            .ToListAsync();

        tables.ShouldBe([
            "AttemptCheckResults",
            "TaskValidationConfigurations",
            "TaskValidationVersions",
            "ValidationChecks"
        ], ignoreOrder: true);

        var constraints = await db.Database
            .SqlQueryRaw<string>(
                """
                SELECT conname AS "Value"
                FROM pg_constraint
                WHERE conname IN (
                    'CK_Attempts_Phase2bScoring',
                    'CK_AttemptCheckResults_BinaryScore')
                """)
            .ToListAsync();

        constraints.ShouldBe([
            "CK_Attempts_Phase2bScoring",
            "CK_AttemptCheckResults_BinaryScore"
        ], ignoreOrder: true);

        var legacyObjects = await db.Database
            .SqlQueryRaw<string>(
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
                """)
            .ToListAsync();
        legacyObjects.ShouldBeEmpty();
    }
}
