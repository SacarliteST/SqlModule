using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class RedesignAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_SqlQueries_QueryId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_QueryId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "EndAttempt",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "IsSuccess",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "QueryId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "StartAttempt",
                table: "Attempts");

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "Attempts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Attempts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinishedAt",
                table: "Attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "Attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Attempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RowCount",
                table: "Attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "Attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Attempts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubmittedSql",
                table: "Attempts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DurationMs", table: "Attempts");
            migrationBuilder.DropColumn(name: "ErrorMessage", table: "Attempts");
            migrationBuilder.DropColumn(name: "FinishedAt", table: "Attempts");
            migrationBuilder.DropColumn(name: "IsCorrect", table: "Attempts");
            migrationBuilder.DropColumn(name: "Reason", table: "Attempts");
            migrationBuilder.DropColumn(name: "RowCount", table: "Attempts");
            migrationBuilder.DropColumn(name: "StartedAt", table: "Attempts");
            migrationBuilder.DropColumn(name: "Status", table: "Attempts");
            migrationBuilder.DropColumn(name: "SubmittedSql", table: "Attempts");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndAttempt",
                table: "Attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "Attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "QueryId",
                table: "Attempts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartAttempt",
                table: "Attempts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_QueryId",
                table: "Attempts",
                column: "QueryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_SqlQueries_QueryId",
                table: "Attempts",
                column: "QueryId",
                principalTable: "SqlQueries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
