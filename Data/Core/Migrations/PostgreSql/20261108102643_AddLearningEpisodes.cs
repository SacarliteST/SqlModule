using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddLearningEpisodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LearningEpisodeId",
                table: "Attempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LearningEpisodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidationVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalizationReason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FinalAttemptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProducesObservation = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningEpisodes", x => x.Id);
                    table.CheckConstraint("CK_LearningEpisodes_Finalization", "(\"Status\" = 'Active' AND \"FinalizedAt\" IS NULL AND \"FinalizationReason\" IS NULL) OR (\"Status\" = 'Finalized' AND \"FinalizedAt\" IS NOT NULL AND \"FinalizationReason\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_LearningEpisodes_Attempts_FinalAttemptId",
                        column: x => x.FinalAttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEpisodes_ModuleSessions_ModuleSessionId",
                        column: x => x.ModuleSessionId,
                        principalTable: "ModuleSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEpisodes_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningEpisodes_TaskValidationVersions_ValidationVersionId",
                        column: x => x.ValidationVersionId,
                        principalTable: "TaskValidationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningEpisodeSkillResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EpisodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningEpisodeSkillResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningEpisodeSkillResults_LearningEpisodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "LearningEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningEpisodeSkillResults_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_LearningEpisodeId",
                table: "Attempts",
                column: "LearningEpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodes_FinalAttemptId",
                table: "LearningEpisodes",
                column: "FinalAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodes_ModuleSessionId",
                table: "LearningEpisodes",
                column: "ModuleSessionId",
                unique: true,
                filter: "\"ModuleSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodes_Status_LastHeartbeatAt",
                table: "LearningEpisodes",
                columns: new[] { "Status", "LastHeartbeatAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodes_StudentId_TaskId",
                table: "LearningEpisodes",
                columns: new[] { "StudentId", "TaskId" },
                unique: true,
                filter: "\"ModuleSessionId\" IS NULL AND \"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodes_TaskId",
                table: "LearningEpisodes",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodes_ValidationVersionId",
                table: "LearningEpisodes",
                column: "ValidationVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodeSkillResults_EpisodeId_SkillId",
                table: "LearningEpisodeSkillResults",
                columns: new[] { "EpisodeId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningEpisodeSkillResults_SkillId",
                table: "LearningEpisodeSkillResults",
                column: "SkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_LearningEpisodes_LearningEpisodeId",
                table: "Attempts",
                column: "LearningEpisodeId",
                principalTable: "LearningEpisodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_LearningEpisodes_LearningEpisodeId",
                table: "Attempts");

            migrationBuilder.DropTable(
                name: "LearningEpisodeSkillResults");

            migrationBuilder.DropTable(
                name: "LearningEpisodes");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_LearningEpisodeId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "LearningEpisodeId",
                table: "Attempts");
        }
    }
}
