using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class MoveAttemptLimitsToModuleSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinalScore",
                table: "ModuleSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalizationReason",
                table: "ModuleSessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<global::System.DateTimeOffset>(
                name: "FinalizedAt",
                table: "ModuleSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "ModuleSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<global::System.Guid>(
                name: "ValidationVersionId",
                table: "ModuleSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                WITH latest_progress AS (
                    SELECT DISTINCT ON (progress."ModuleSessionId")
                           progress."ModuleSessionId",
                           progress."ValidationVersionId",
                           progress."FinalScore",
                           progress."FinalizationReason",
                           progress."FinalizedAt",
                           version."MaxAttempts"
                    FROM "StudentTaskProgresses" AS progress
                    JOIN "TaskValidationVersions" AS version
                      ON version."Id" = progress."ValidationVersionId"
                    WHERE progress."ModuleSessionId" IS NOT NULL
                    ORDER BY progress."ModuleSessionId", progress."UpdatedAt" DESC, progress."Id" DESC
                )
                UPDATE "ModuleSessions" AS session
                SET "ValidationVersionId" = progress."ValidationVersionId",
                    "MaxAttempts" = progress."MaxAttempts",
                    "FinalScore" = progress."FinalScore",
                    "FinalizationReason" = CASE
                        WHEN progress."FinalizationReason" = 'Restarted' THEN 'Manual'
                        ELSE progress."FinalizationReason"
                    END,
                    "FinalizedAt" = progress."FinalizedAt"
                FROM latest_progress AS progress
                WHERE progress."ModuleSessionId" = session."SessionId";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ModuleSessions_FinalScore",
                table: "ModuleSessions",
                sql: "\"FinalScore\" IS NULL OR \"FinalScore\" BETWEEN 0 AND 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ModuleSessions_MaxAttempts",
                table: "ModuleSessions",
                sql: "\"MaxAttempts\" IS NULL OR \"MaxAttempts\" > 0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attempts_Phase2bScoring",
                table: "Attempts");

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "ModuleSessionId"
                               ORDER BY "FinishedAt", "CreatedAt", "Id") AS number
                    FROM "Attempts"
                    WHERE "ModuleSessionId" IS NOT NULL
                      AND "ValidationVersionId" IS NOT NULL
                )
                UPDATE "Attempts" AS attempt
                SET "AttemptNumber" = numbered.number
                FROM numbered
                WHERE numbered."Id" = attempt."Id";
                """);

            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "UserId", "TaskId"
                               ORDER BY "FinishedAt", "CreatedAt", "Id") AS number
                    FROM "Attempts"
                    WHERE "ModuleSessionId" IS NULL
                      AND "ValidationVersionId" IS NOT NULL
                )
                UPDATE "Attempts" AS attempt
                SET "AttemptNumber" = numbered.number
                FROM numbered
                WHERE numbered."Id" = attempt."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ModuleSessionId_AttemptNumber",
                table: "Attempts",
                columns: new[] { "ModuleSessionId", "AttemptNumber" },
                unique: true,
                filter: "\"ModuleSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_TaskId_AttemptNumber",
                table: "Attempts",
                columns: new[] { "UserId", "TaskId", "AttemptNumber" },
                unique: true,
                filter: "\"ModuleSessionId\" IS NULL AND \"AttemptNumber\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attempts_Phase2bScoring",
                table: "Attempts",
                sql: "(\"ValidationVersionId\" IS NULL AND \"AttemptNumber\" IS NULL AND \"Score\" IS NULL) OR (\"ValidationVersionId\" IS NOT NULL AND \"AttemptNumber\" IS NOT NULL AND \"Score\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ModuleSessions_FinalScore",
                table: "ModuleSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ModuleSessions_MaxAttempts",
                table: "ModuleSessions");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ModuleSessionId_AttemptNumber",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_UserId_TaskId_AttemptNumber",
                table: "Attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attempts_Phase2bScoring",
                table: "Attempts");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attempts_Phase2bScoring",
                table: "Attempts",
                sql: "(\"ProgressId\" IS NULL AND \"ValidationVersionId\" IS NULL AND \"AttemptNumber\" IS NULL AND \"Score\" IS NULL) OR (\"ProgressId\" IS NOT NULL AND \"ValidationVersionId\" IS NOT NULL AND \"AttemptNumber\" IS NOT NULL AND \"Score\" IS NOT NULL)");

            migrationBuilder.DropColumn(name: "FinalScore", table: "ModuleSessions");
            migrationBuilder.DropColumn(name: "FinalizationReason", table: "ModuleSessions");
            migrationBuilder.DropColumn(name: "FinalizedAt", table: "ModuleSessions");
            migrationBuilder.DropColumn(name: "MaxAttempts", table: "ModuleSessions");
            migrationBuilder.DropColumn(name: "ValidationVersionId", table: "ModuleSessions");
        }
    }
}
