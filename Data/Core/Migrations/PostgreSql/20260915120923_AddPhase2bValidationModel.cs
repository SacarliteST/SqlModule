using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddPhase2bValidationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveValidationVersionId",
                table: "SqlTasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "Attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CountsTowardLimit",
                table: "Attempts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgressId",
                table: "Attempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ValidationVersionId",
                table: "Attempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttemptCheckResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidationCheckId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    AwardedScore = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DiagnosticJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptCheckResults", x => x.Id);
                    table.CheckConstraint("CK_AttemptCheckResults_AwardedScore", "\"AwardedScore\" >= 0 AND \"AwardedScore\" <= \"Weight\"");
                    table.CheckConstraint("CK_AttemptCheckResults_BinaryScore", "(\"Status\" = 'Passed' AND \"AwardedScore\" = \"Weight\") OR (\"Status\" IN ('Failed', 'NotEvaluated') AND \"AwardedScore\" = 0)");
                    table.CheckConstraint("CK_AttemptCheckResults_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_AttemptCheckResults_Weight", "\"Weight\" BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_AttemptCheckResults_Attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskValidationConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassingScore = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: true),
                    VisibleHintGroupsMask = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskValidationConfigurations", x => x.Id);
                    table.CheckConstraint("CK_TaskValidationConfigurations_HintMask", "\"VisibleHintGroupsMask\" >= 0");
                    table.CheckConstraint("CK_TaskValidationConfigurations_MaxAttempts", "\"MaxAttempts\" IS NULL OR \"MaxAttempts\" > 0");
                    table.CheckConstraint("CK_TaskValidationConfigurations_PassingScore", "\"PassingScore\" BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_TaskValidationConfigurations_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskValidationVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ConfigurationVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    PassingScore = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: true),
                    VisibleHintGroupsMask = table.Column<long>(type: "bigint", nullable: false),
                    SchemaSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    DatasetSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReferenceQuerySnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExpectedResultSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationConfigurationSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    AnalyzerVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedById = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskValidationVersions", x => x.Id);
                    table.CheckConstraint("CK_TaskValidationVersions_HintMask", "\"VisibleHintGroupsMask\" >= 0");
                    table.CheckConstraint("CK_TaskValidationVersions_MaxAttempts", "\"MaxAttempts\" IS NULL OR \"MaxAttempts\" > 0");
                    table.CheckConstraint("CK_TaskValidationVersions_Number", "\"VersionNumber\" > 0");
                    table.CheckConstraint("CK_TaskValidationVersions_PassingScore", "\"PassingScore\" BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_TaskValidationVersions_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UniquenessValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationChecks", x => x.Id);
                    table.CheckConstraint("CK_ValidationChecks_Order", "\"Order\" >= 0");
                    table.CheckConstraint("CK_ValidationChecks_Weight", "\"Weight\" BETWEEN 1 AND 100");
                    table.ForeignKey(
                        name: "FK_ValidationChecks_TaskValidationConfigurations_Configuration~",
                        column: x => x.ConfigurationId,
                        principalTable: "TaskValidationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentTaskProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ValidationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptsUsed = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    BestScore = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FinalScore = table.Column<int>(type: "integer", nullable: true),
                    FinalizationReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyVersion = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTaskProgresses", x => x.Id);
                    table.CheckConstraint("CK_StudentTaskProgresses_AttemptsUsed", "\"AttemptsUsed\" >= 0");
                    table.CheckConstraint("CK_StudentTaskProgresses_BestScore", "\"BestScore\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_StudentTaskProgresses_Finalization", "(\"FinalScore\" IS NULL AND \"FinalizationReason\" IS NULL AND \"FinalizedAt\" IS NULL) OR (\"FinalScore\" IS NOT NULL AND \"FinalizationReason\" IS NOT NULL AND \"FinalizedAt\" IS NOT NULL)");
                    table.CheckConstraint("CK_StudentTaskProgresses_FinalScore", "\"FinalScore\" IS NULL OR \"FinalScore\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_StudentTaskProgresses_NextAttemptNumber", "\"NextAttemptNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_StudentTaskProgresses_ModuleSessions_ModuleSessionId",
                        column: x => x.ModuleSessionId,
                        principalTable: "ModuleSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentTaskProgresses_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentTaskProgresses_TaskValidationVersions_ValidationVers~",
                        column: x => x.ValidationVersionId,
                        principalTable: "TaskValidationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttemptReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptReservations", x => x.Id);
                    table.CheckConstraint("CK_AttemptReservations_Number", "\"AttemptNumber\" > 0");
                    table.ForeignKey(
                        name: "FK_AttemptReservations_Attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttemptReservations_StudentTaskProgresses_ProgressId",
                        column: x => x.ProgressId,
                        principalTable: "StudentTaskProgresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_ActiveValidationVersionId",
                table: "SqlTasks",
                column: "ActiveValidationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ProgressId_AttemptNumber",
                table: "Attempts",
                columns: new[] { "ProgressId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ValidationVersionId",
                table: "Attempts",
                column: "ValidationVersionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attempts_AttemptNumber",
                table: "Attempts",
                sql: "\"AttemptNumber\" IS NULL OR \"AttemptNumber\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attempts_Phase2bScoring",
                table: "Attempts",
                sql: "(\"ProgressId\" IS NULL AND \"ValidationVersionId\" IS NULL AND \"AttemptNumber\" IS NULL AND \"Score\" IS NULL) OR (\"ProgressId\" IS NOT NULL AND \"ValidationVersionId\" IS NOT NULL AND \"AttemptNumber\" IS NOT NULL AND \"Score\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attempts_Score",
                table: "Attempts",
                sql: "\"Score\" IS NULL OR \"Score\" BETWEEN 0 AND 100");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptCheckResults_AttemptId_Order",
                table: "AttemptCheckResults",
                columns: new[] { "AttemptId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptCheckResults_ValidationCheckId",
                table: "AttemptCheckResults",
                column: "ValidationCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptReservations_AttemptId",
                table: "AttemptReservations",
                column: "AttemptId",
                unique: true,
                filter: "\"AttemptId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptReservations_ProgressId_AttemptNumber",
                table: "AttemptReservations",
                columns: new[] { "ProgressId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptReservations_ProgressId_IdempotencyKey",
                table: "AttemptReservations",
                columns: new[] { "ProgressId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttemptReservations_State_UpdatedAt",
                table: "AttemptReservations",
                columns: new[] { "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskProgresses_ModuleSessionId",
                table: "StudentTaskProgresses",
                column: "ModuleSessionId",
                unique: true,
                filter: "\"ModuleSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskProgresses_Status_ExpiresAt",
                table: "StudentTaskProgresses",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskProgresses_TaskId",
                table: "StudentTaskProgresses",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskProgresses_UserId_TaskId",
                table: "StudentTaskProgresses",
                columns: new[] { "UserId", "TaskId" },
                unique: true,
                filter: "\"ModuleSessionId\" IS NULL AND \"Status\" IN ('Active', 'Finalizing', 'CompletionPending', 'CompletionFailed')");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTaskProgresses_ValidationVersionId",
                table: "StudentTaskProgresses",
                column: "ValidationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskValidationConfigurations_TaskId",
                table: "TaskValidationConfigurations",
                column: "TaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskValidationVersions_TaskId_ConfigurationVersion",
                table: "TaskValidationVersions",
                columns: new[] { "TaskId", "ConfigurationVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskValidationVersions_TaskId_VersionNumber",
                table: "TaskValidationVersions",
                columns: new[] { "TaskId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationChecks_ConfigurationId_Kind_UniquenessValue",
                table: "ValidationChecks",
                columns: new[] { "ConfigurationId", "Kind", "UniquenessValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationChecks_ConfigurationId_Order",
                table: "ValidationChecks",
                columns: new[] { "ConfigurationId", "Order" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "TaskValidationConfigurations"
                    ("Id", "TaskId", "PassingScore", "MaxAttempts", "VisibleHintGroupsMask", "Version",
                     "CreatedById", "CreatedByName", "CreatedAt", "UpdatedById", "UpdatedByName", "UpdatedAt")
                SELECT gen_random_uuid(), task."Id", 100, NULL, 1, gen_random_uuid(),
                       task."CreatedById", task."CreatedByName", task."CreatedAt",
                       task."UpdatedById", task."UpdatedByName", task."UpdatedAt"
                FROM "SqlTasks" AS task;

                INSERT INTO "ValidationChecks"
                    ("Id", "ConfigurationId", "Kind", "Value", "UniquenessValue", "Weight", "Order")
                SELECT gen_random_uuid(), configuration."Id", 'MainDatasetResult', NULL, '', 100, 0
                FROM "TaskValidationConfigurations" AS configuration;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_StudentTaskProgresses_ProgressId",
                table: "Attempts",
                column: "ProgressId",
                principalTable: "StudentTaskProgresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_TaskValidationVersions_ValidationVersionId",
                table: "Attempts",
                column: "ValidationVersionId",
                principalTable: "TaskValidationVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SqlTasks_TaskValidationVersions_ActiveValidationVersionId",
                table: "SqlTasks",
                column: "ActiveValidationVersionId",
                principalTable: "TaskValidationVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_StudentTaskProgresses_ProgressId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_TaskValidationVersions_ValidationVersionId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_SqlTasks_TaskValidationVersions_ActiveValidationVersionId",
                table: "SqlTasks");

            migrationBuilder.DropTable(
                name: "AttemptCheckResults");

            migrationBuilder.DropTable(
                name: "AttemptReservations");

            migrationBuilder.DropTable(
                name: "ValidationChecks");

            migrationBuilder.DropTable(
                name: "StudentTaskProgresses");

            migrationBuilder.DropTable(
                name: "TaskValidationConfigurations");

            migrationBuilder.DropTable(
                name: "TaskValidationVersions");

            migrationBuilder.DropIndex(
                name: "IX_SqlTasks_ActiveValidationVersionId",
                table: "SqlTasks");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ProgressId_AttemptNumber",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ValidationVersionId",
                table: "Attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attempts_AttemptNumber",
                table: "Attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attempts_Phase2bScoring",
                table: "Attempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attempts_Score",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ActiveValidationVersionId",
                table: "SqlTasks");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "CountsTowardLimit",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ProgressId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ValidationVersionId",
                table: "Attempts");
        }
    }
}
