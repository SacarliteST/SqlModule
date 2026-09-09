using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class EnforceSqlTaskSqlQueryOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Historical data may reuse one reference query for several tasks. Preserve every
            // task by cloning the complete reference query (including the golden result and audit
            // fields) for all but the first task before enforcing the one-to-one constraint.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    duplicate_task RECORD;
                    cloned_query_id uuid;
                BEGIN
                    FOR duplicate_task IN
                        SELECT "Id", "SqlQueryId"
                        FROM (
                            SELECT
                                "Id",
                                "SqlQueryId",
                                row_number() OVER (
                                    PARTITION BY "SqlQueryId"
                                    ORDER BY "CreatedAt", "Id") AS occurrence
                            FROM "SqlTasks"
                        ) AS ranked_tasks
                        WHERE occurrence > 1
                    LOOP
                        cloned_query_id := gen_random_uuid();

                        INSERT INTO "SqlQueries" (
                            "Id",
                            "TargetDbId",
                            "QueryText",
                            "StrictColumnOrder",
                            "StrictRowOrder",
                            "ExpectedResult",
                            "CreatedById",
                            "CreatedAt",
                            "UpdatedById",
                            "UpdatedAt")
                        SELECT
                            cloned_query_id,
                            "TargetDbId",
                            "QueryText",
                            "StrictColumnOrder",
                            "StrictRowOrder",
                            "ExpectedResult",
                            "CreatedById",
                            "CreatedAt",
                            "UpdatedById",
                            "UpdatedAt"
                        FROM "SqlQueries"
                        WHERE "Id" = duplicate_task."SqlQueryId";

                        UPDATE "SqlTasks"
                        SET "SqlQueryId" = cloned_query_id
                        WHERE "Id" = duplicate_task."Id";
                    END LOOP;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_SqlTasks_SqlQueryId",
                table: "SqlTasks");

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_SqlQueryId",
                table: "SqlTasks",
                column: "SqlQueryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SqlTasks_SqlQueryId",
                table: "SqlTasks");

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_SqlQueryId",
                table: "SqlTasks",
                column: "SqlQueryId");
        }
    }
}
