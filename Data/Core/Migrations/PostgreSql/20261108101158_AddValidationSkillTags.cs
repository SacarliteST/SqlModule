using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddValidationSkillTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskValidationSkillTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskValidationSkillTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskValidationSkillTags_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskValidationSkillTags_TaskValidationConfigurations_Config~",
                        column: x => x.ConfigurationId,
                        principalTable: "TaskValidationConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskValidationSkillTags_ConfigurationId_SkillId",
                table: "TaskValidationSkillTags",
                columns: new[] { "ConfigurationId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskValidationSkillTags_SkillId",
                table: "TaskValidationSkillTags",
                column: "SkillId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskValidationSkillTags");
        }
    }
}
