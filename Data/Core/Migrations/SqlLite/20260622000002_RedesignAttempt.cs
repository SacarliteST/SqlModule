using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.SqlLite
{
    /// <inheritdoc />
    public partial class RedesignAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Attempts",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinishedAt",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrect",
                table: "Attempts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RowCount",
                table: "Attempts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubmittedSql",
                table: "Attempts",
                type: "TEXT",
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
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "Attempts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "QueryId",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartAttempt",
                table: "Attempts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_QueryId",
                table: "Attempts",
                column: "QueryId");
        }
    }
}
