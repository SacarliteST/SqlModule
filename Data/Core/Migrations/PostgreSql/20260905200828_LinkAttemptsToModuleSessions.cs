using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class LinkAttemptsToModuleSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ModuleSessionId",
                table: "Attempts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_ModuleSessionId",
                table: "Attempts",
                column: "ModuleSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_ModuleSessions_ModuleSessionId",
                table: "Attempts",
                column: "ModuleSessionId",
                principalTable: "ModuleSessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_ModuleSessions_ModuleSessionId",
                table: "Attempts");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_ModuleSessionId",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "ModuleSessionId",
                table: "Attempts");
        }
    }
}
