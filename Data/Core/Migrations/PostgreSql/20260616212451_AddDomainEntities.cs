using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQLModule.Data.Core.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DbmsDictionaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DbmsName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DbmsSystemName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DockerImage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultPort = table.Column<int>(type: "integer", nullable: false),
                    EnvUserKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvPasswordKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnvDatabaseKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExtraEnvConfig = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultDatabase = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefaultUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DefaultPassword = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbmsDictionaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SqlQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryText = table.Column<string>(type: "text", nullable: false),
                    StrictColumnOrder = table.Column<bool>(type: "boolean", nullable: false),
                    StrictRowOrder = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqlQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ParentTopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Topics_Topics_ParentTopicId",
                        column: x => x.ParentTopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DbmsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalTypes_DbmsDictionaries_DbmsId",
                        column: x => x.DbmsId,
                        principalTable: "DbmsDictionaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TargetDbs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DbmsId = table.Column<Guid>(type: "uuid", nullable: false),
                    DbName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsReadOnly = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetDbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetDbs_DbmsDictionaries_DbmsId",
                        column: x => x.DbmsId,
                        principalTable: "DbmsDictionaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicalTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InputType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    SqlFragment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ValuePrefix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValueSuffix = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Separator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterDefinitions_PhysicalTypes_PhysicalTypeId",
                        column: x => x.PhysicalTypeId,
                        principalTable: "PhysicalTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetaTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDbId = table.Column<Guid>(type: "uuid", nullable: false),
                    TableName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaTables_TargetDbs_TargetDbId",
                        column: x => x.TargetDbId,
                        principalTable: "TargetDbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SqlTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDbId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    SqlQueryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TaskText = table.Column<string>(type: "text", nullable: false),
                    DifficultyLevel = table.Column<short>(type: "smallint", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqlTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SqlTasks_SqlQueries_SqlQueryId",
                        column: x => x.SqlQueryId,
                        principalTable: "SqlQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SqlTasks_TargetDbs_TargetDbId",
                        column: x => x.TargetDbId,
                        principalTable: "TargetDbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SqlTasks_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataRecords_MetaTables_MetaTableId",
                        column: x => x.MetaTableId,
                        principalTable: "MetaTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetaAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhysicalTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsPrimaryKey = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaAttributes_MetaTables_MetaTableId",
                        column: x => x.MetaTableId,
                        principalTable: "MetaTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MetaAttributes_PhysicalTypes_PhysicalTypeId",
                        column: x => x.PhysicalTypeId,
                        principalTable: "PhysicalTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    StartAttempt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAttempt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attempts_SqlQueries_QueryId",
                        column: x => x.QueryId,
                        principalTable: "SqlQueries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attempts_SqlTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "SqlTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttributeParameterValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParameterValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeParameterValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeParameterValues_MetaAttributes_MetaAttributeId",
                        column: x => x.MetaAttributeId,
                        principalTable: "MetaAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttributeParameterValues_ParameterDefinitions_ParameterDefi~",
                        column: x => x.ParameterDefinitionId,
                        principalTable: "ParameterDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CellValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    MetaAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextValue = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CellValues_DataRecords_DataRecordId",
                        column: x => x.DataRecordId,
                        principalTable: "DataRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CellValues_MetaAttributes_MetaAttributeId",
                        column: x => x.MetaAttributeId,
                        principalTable: "MetaAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetaRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeleteRule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdateRule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetaRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetaRelationships_MetaAttributes_SourceAttributeId",
                        column: x => x.SourceAttributeId,
                        principalTable: "MetaAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetaRelationships_MetaAttributes_TargetAttributeId",
                        column: x => x.TargetAttributeId,
                        principalTable: "MetaAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_QueryId",
                table: "Attempts",
                column: "QueryId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_TaskId",
                table: "Attempts",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeParameterValues_MetaAttributeId",
                table: "AttributeParameterValues",
                column: "MetaAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeParameterValues_ParameterDefinitionId",
                table: "AttributeParameterValues",
                column: "ParameterDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CellValues_DataRecordId",
                table: "CellValues",
                column: "DataRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CellValues_MetaAttributeId",
                table: "CellValues",
                column: "MetaAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_DataRecords_MetaTableId",
                table: "DataRecords",
                column: "MetaTableId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaAttributes_MetaTableId",
                table: "MetaAttributes",
                column: "MetaTableId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaAttributes_PhysicalTypeId",
                table: "MetaAttributes",
                column: "PhysicalTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaRelationships_SourceAttributeId",
                table: "MetaRelationships",
                column: "SourceAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaRelationships_TargetAttributeId",
                table: "MetaRelationships",
                column: "TargetAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_MetaTables_TargetDbId",
                table: "MetaTables",
                column: "TargetDbId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterDefinitions_PhysicalTypeId",
                table: "ParameterDefinitions",
                column: "PhysicalTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalTypes_DbmsId",
                table: "PhysicalTypes",
                column: "DbmsId");

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_SqlQueryId",
                table: "SqlTasks",
                column: "SqlQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_TargetDbId",
                table: "SqlTasks",
                column: "TargetDbId");

            migrationBuilder.CreateIndex(
                name: "IX_SqlTasks_TopicId",
                table: "SqlTasks",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetDbs_DbmsId",
                table: "TargetDbs",
                column: "DbmsId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_ParentTopicId",
                table: "Topics",
                column: "ParentTopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attempts");

            migrationBuilder.DropTable(
                name: "AttributeParameterValues");

            migrationBuilder.DropTable(
                name: "CellValues");

            migrationBuilder.DropTable(
                name: "MetaRelationships");

            migrationBuilder.DropTable(
                name: "SqlTasks");

            migrationBuilder.DropTable(
                name: "ParameterDefinitions");

            migrationBuilder.DropTable(
                name: "DataRecords");

            migrationBuilder.DropTable(
                name: "MetaAttributes");

            migrationBuilder.DropTable(
                name: "SqlQueries");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "MetaTables");

            migrationBuilder.DropTable(
                name: "PhysicalTypes");

            migrationBuilder.DropTable(
                name: "TargetDbs");

            migrationBuilder.DropTable(
                name: "DbmsDictionaries");
        }
    }
}
