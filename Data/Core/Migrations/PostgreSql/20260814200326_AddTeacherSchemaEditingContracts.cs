using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddTeacherSchemaEditingContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Topics",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SchemaVersion",
                table: "TargetDbs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<short>(
                name: "SortOrder",
                table: "MetaTables",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "MutationReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutationReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MutationReceipts_Scope_IdempotencyKey",
                table: "MutationReceipts",
                columns: new[] { "Scope", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MutationReceipts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "SchemaVersion",
                table: "TargetDbs");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "MetaTables");
        }
    }
}
