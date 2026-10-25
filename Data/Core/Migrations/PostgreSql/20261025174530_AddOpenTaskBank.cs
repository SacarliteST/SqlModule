using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddOpenTaskBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "SqlTaskBankNumberSequence");

            migrationBuilder.AddColumn<string>(
                name: "BankHint",
                table: "SqlTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BankNumber",
                table: "SqlTasks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BankPublishedAt",
                table: "SqlTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowSolution",
                table: "SqlTasks",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SolutionExplanation",
                table: "SqlTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "SqlTasks",
                type: "text",
                nullable: false,
                defaultValue: "Control");

            migrationBuilder.CreateTable(
                name: "BankHintAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    AutoHintGroupsMask = table.Column<long>(type: "bigint", nullable: false),
                    AuthorHintIssued = table.Column<bool>(type: "boolean", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankHintAccesses", x => x.Id);
                    table.CheckConstraint("CK_BankHintAccesses_AttemptNumber", "\"AttemptNumber\" > 0");
                    table.CheckConstraint("CK_BankHintAccesses_HintMask", "\"AutoHintGroupsMask\" >= 0");
                    table.ForeignKey(
                        name: "FK_BankHintAccesses_Attempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankHintAccesses_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankSolutionAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankSolutionAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankSolutionAccesses_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_BankNumber",
                table: "SqlTasks",
                column: "BankNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_Visibility_PublicationStatus_BankPublishedAt",
                table: "SqlTasks",
                columns: new[] { "Visibility", "PublicationStatus", "BankPublishedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_SqlTasks_BankPublication",
                table: "SqlTasks",
                sql: "(\"BankNumber\" IS NULL AND \"BankPublishedAt\" IS NULL) OR (\"BankNumber\" IS NOT NULL AND \"BankNumber\" > 0 AND \"BankPublishedAt\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_TaskId_ModuleSessionId_Score",
                table: "Attempts",
                columns: new[] { "UserId", "TaskId", "ModuleSessionId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_BankHintAccesses_AttemptId",
                table: "BankHintAccesses",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_BankHintAccesses_TaskId_RequestedAt",
                table: "BankHintAccesses",
                columns: new[] { "TaskId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BankHintAccesses_UserId_TaskId_RequestedAt",
                table: "BankHintAccesses",
                columns: new[] { "UserId", "TaskId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BankSolutionAccesses_TaskId_OpenedAt",
                table: "BankSolutionAccesses",
                columns: new[] { "TaskId", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BankSolutionAccesses_UserId_TaskId_OpenedAt",
                table: "BankSolutionAccesses",
                columns: new[] { "UserId", "TaskId", "OpenedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
