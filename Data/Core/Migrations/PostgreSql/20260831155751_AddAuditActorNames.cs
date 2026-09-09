using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddAuditActorNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Topics",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "Topics",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "TargetDbs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "TargetDbs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "SqlTasks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "SqlTasks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "SqlQueries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "SqlQueries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "PhysicalTypes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "PhysicalTypes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "ParameterDefinitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "ParameterDefinitions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MetaTables",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "MetaTables",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MetaRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "MetaRelationships",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "MetaAttributes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "MetaAttributes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "DbmsDictionaries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "DbmsDictionaries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "DataRecords",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "DataRecords",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "CellValues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "CellValues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "AttributeParameterValues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "AttributeParameterValues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "Attempts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByName",
                table: "Attempts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    audit_table text;
                BEGIN
                    FOREACH audit_table IN ARRAY ARRAY[
                        'Topics', 'TargetDbs', 'SqlTasks', 'SqlQueries', 'PhysicalTypes',
                        'ParameterDefinitions', 'MetaTables', 'MetaRelationships', 'MetaAttributes',
                        'DbmsDictionaries', 'DataRecords', 'CellValues', 'AttributeParameterValues', 'Attempts'
                    ]
                    LOOP
                        EXECUTE format(
                            'UPDATE %I SET
                                "CreatedAt" = CASE WHEN "CreatedAt" <= ''0001-01-02''::timestamptz THEN CURRENT_TIMESTAMP ELSE "CreatedAt" END,
                                "UpdatedAt" = CASE WHEN "UpdatedAt" <= ''0001-01-02''::timestamptz THEN
                                    CASE WHEN "CreatedAt" <= ''0001-01-02''::timestamptz THEN CURRENT_TIMESTAMP ELSE "CreatedAt" END
                                    ELSE "UpdatedAt" END,
                                "CreatedById" = CASE WHEN "CreatedById" = ''00000000-0000-0000-0000-000000000000''::uuid
                                    THEN ''00000000-0000-0000-0000-000000000001''::uuid ELSE "CreatedById" END,
                                "UpdatedById" = CASE WHEN "UpdatedById" = ''00000000-0000-0000-0000-000000000000''::uuid
                                    THEN CASE WHEN "CreatedById" = ''00000000-0000-0000-0000-000000000000''::uuid
                                        THEN ''00000000-0000-0000-0000-000000000001''::uuid ELSE "CreatedById" END
                                    ELSE "UpdatedById" END,
                                "CreatedByName" = COALESCE("CreatedByName",
                                    CASE WHEN "CreatedById" IN (
                                        ''00000000-0000-0000-0000-000000000000''::uuid,
                                        ''00000000-0000-0000-0000-000000000001''::uuid)
                                    THEN ''Система'' ELSE "CreatedById"::text END),
                                "UpdatedByName" = COALESCE("UpdatedByName",
                                    CASE WHEN "UpdatedById" IN (
                                        ''00000000-0000-0000-0000-000000000000''::uuid,
                                        ''00000000-0000-0000-0000-000000000001''::uuid)
                                    THEN ''Система'' ELSE "UpdatedById"::text END)',
                            audit_table);
                    END LOOP;

                    UPDATE "Attempts"
                    SET "CreatedAt" = "StartedAt",
                        "UpdatedAt" = "FinishedAt",
                        "CreatedById" = "UserId",
                        "UpdatedById" = "UserId",
                        "CreatedByName" = COALESCE(NULLIF("StudentName", ''), "UserId"::text),
                        "UpdatedByName" = COALESCE(NULLIF("StudentName", ''), "UserId"::text);
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "Topics");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "TargetDbs");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "TargetDbs");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "SqlTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "SqlTasks");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "SqlQueries");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "SqlQueries");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "PhysicalTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "PhysicalTypes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "ParameterDefinitions");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "ParameterDefinitions");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MetaTables");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "MetaTables");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MetaRelationships");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "MetaRelationships");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "MetaAttributes");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "MetaAttributes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "DbmsDictionaries");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "DbmsDictionaries");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "DataRecords");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "DataRecords");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "CellValues");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "CellValues");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "AttributeParameterValues");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "AttributeParameterValues");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "Attempts");

            migrationBuilder.DropColumn(
                name: "UpdatedByName",
                table: "Attempts");
        }
    }
}
