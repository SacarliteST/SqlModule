using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddAttemptResultSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActualColumnsJson",
                table: "Attempts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualRowsJson",
                table: "Attempts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsResultTruncated",
                table: "Attempts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ResultRowLimit",
                table: "Attempts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultSnapshotCreatedAt",
                table: "Attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultSnapshotExpiresAt",
                table: "Attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultSnapshotState",
                table: "Attempts",
                type: "text",
                nullable: false,
                defaultValue: "NotStored");

            migrationBuilder.AddColumn<int>(
                name: "ReturnedRowCount",
                table: "Attempts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualColumnsJson",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ActualRowsJson",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "IsResultTruncated",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ResultRowLimit",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ResultSnapshotCreatedAt",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ResultSnapshotExpiresAt",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ResultSnapshotState",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ReturnedRowCount",
                table: "Attempts");
        }
    }
}
