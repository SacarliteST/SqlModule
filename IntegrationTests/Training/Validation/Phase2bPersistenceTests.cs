using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Data.Core;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.Validation;

[Collection(IntegrationTestCollection.Name)]
public sealed class Phase2bPersistenceTests(TestApplication app)
{
    [Fact(DisplayName = "Migration создаёт таблицы и ключевые ограничения Phase 2b")]
    public async Task Migration_CreatesPhase2bSchema()
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
                    'StudentTaskProgresses',
                    'AttemptReservations',
                    'AttemptCheckResults')
                """)
            .ToListAsync();

        tables.ShouldBe([
            "AttemptCheckResults",
            "AttemptReservations",
            "StudentTaskProgresses",
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
                    'CK_AttemptCheckResults_BinaryScore',
                    'CK_StudentTaskProgresses_Finalization')
                """)
            .ToListAsync();

        constraints.ShouldBe([
            "CK_Attempts_Phase2bScoring",
            "CK_AttemptCheckResults_BinaryScore",
            "CK_StudentTaskProgresses_Finalization"
        ], ignoreOrder: true);
    }
}
