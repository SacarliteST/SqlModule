using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql;

/// <inheritdoc />
public partial class RemoveLegacyProgress : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Attempts_StudentTaskProgresses_ProgressId",
            table: "Attempts");

        migrationBuilder.DropTable(name: "AttemptReservations");
        migrationBuilder.DropTable(name: "StudentTaskProgresses");

        migrationBuilder.DropCheckConstraint(
            name: "CK_TaskValidationVersions_MaxAttempts",
            table: "TaskValidationVersions");
        migrationBuilder.DropCheckConstraint(
            name: "CK_TaskValidationConfigurations_MaxAttempts",
            table: "TaskValidationConfigurations");

        migrationBuilder.DropIndex(
            name: "IX_Attempts_ProgressId_AttemptNumber",
            table: "Attempts");

        migrationBuilder.DropColumn(name: "MaxAttempts", table: "TaskValidationVersions");
        migrationBuilder.DropColumn(name: "MaxAttempts", table: "TaskValidationConfigurations");
        migrationBuilder.DropColumn(name: "ProgressId", table: "Attempts");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
