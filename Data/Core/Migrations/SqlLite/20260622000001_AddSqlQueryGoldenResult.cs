using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.SqlLite
{
    /// <inheritdoc />
    public partial class AddSqlQueryGoldenResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedResult",
                table: "SqlQueries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetDbId",
                table: "SqlQueries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SqlQueries_TargetDbId",
                table: "SqlQueries",
                column: "TargetDbId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SqlQueries_TargetDbId",
                table: "SqlQueries");

            migrationBuilder.DropColumn(
                name: "ExpectedResult",
                table: "SqlQueries");

            migrationBuilder.DropColumn(
                name: "TargetDbId",
                table: "SqlQueries");
        }
    }
}
