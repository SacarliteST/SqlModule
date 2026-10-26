using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class EnforceOpenTaskBankState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_SqlTasks_OpenBankState",
                table: "SqlTasks",
                sql: "\"Visibility\" <> 'Open' OR (\"BankNumber\" IS NOT NULL AND \"BankPublishedAt\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SqlTasks_OpenBankState",
                table: "SqlTasks");
        }
    }
}
