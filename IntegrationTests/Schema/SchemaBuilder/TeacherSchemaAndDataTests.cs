using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Isolated;

namespace SQLModule.IntegrationTests.Schema.SchemaBuilder;

[Collection(IntegrationTestCollection.Name)]
public sealed class TeacherSchemaAndDataTests(TestApplication app) : ApiTestBase(app)
{
    public static TheoryData<string[], long> NullableCellCases => new()
    {
        { [], 0 },
        { ["NULL", "NULL"], 2 },
        { ["MISSING", "MISSING"], 2 },
        { ["NULL", "MISSING", "value"], 2 },
        { ["one", "two", "three"], 0 },
        { [String.Empty, "NULL"], 1 }
    };

    [Fact(DisplayName = "Schema diff сохраняет UUID и EAV при безопасных изменениях заполненной таблицы")]
    public async Task SchemaDiff_SafeChanges_PreserveExistingData()
    {
        var fixture = await CreateFixtureAsync();
        var initial = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId, twoColumns: true), "diff-initial");
        var table = initial.Tables.Single();
        var filledColumn = table.Columns[0];
        var emptyColumn = table.Columns[1];
        var saved = await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId, table.Id, "diff-row",
            new BatchTableRowsRequest
            {
                SchemaVersion = initial.Version,
                Changes =
                [
                    new TableRowChange
                    {
                        Operation = TableRowOperation.Create, TempId = "row", SortOrder = 0,
                        Cells = new Dictionary<Guid, TableCellRequest>
                        {
                            [filledColumn.Id] = new() { Value = "kept", IsNull = false }
                        }
                    }
                ]
            });

        var addNullable = ExistingSchemaRequest(initial, fixture.PhysicalTypeId, includeEmptyColumn: true,
            newColumnRequired: false, addTable: true);
        var validation = await SchemaBuilderClient.ValidateTargetDbSchemaAsync(fixture.TargetDbId, addNullable);
        validation.IsValid.ShouldBeTrue();
        validation.Changes.ShouldContain(x => x.Kind == "Add" && x.EntityType == "Column");
        var updated = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, addNullable, "diff-add-nullable");

        var updatedTable = updated.Tables.Single(x => x.Id == table.Id);
        updated.Tables.ShouldContain(x => x.Name == "added_table");
        updatedTable.Columns.ShouldContain(x => x.Id == filledColumn.Id);
        updatedTable.Columns.ShouldContain(x => x.Id == emptyColumn.Id);
        updatedTable.Description.ShouldBe("Updated description");
        var rows = await SchemaBuilderClient.GetTableRowsAsync(fixture.TargetDbId, table.Id, 0, 50);
        rows!.Count.ShouldBe(1);
        rows.Items.Single().Id.ShouldBe(saved.Rows.Single().Id);
        rows.Items.Single().Cells[filledColumn.Id].Value.ShouldBe("kept");

        var removeEmpty = ExistingSchemaRequest(updated, fixture.PhysicalTypeId, includeEmptyColumn: false,
            newColumnRequired: null);
        var afterRemoval = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, removeEmpty, "diff-remove-empty");
        afterRemoval.Tables.Single().Columns.ShouldNotContain(x => x.Id == emptyColumn.Id);
        (await SchemaBuilderClient.GetTableRowsAsync(fixture.TargetDbId, table.Id, 0, 50))!.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Schema diff точно блокирует опасные изменения заполненной таблицы")]
    public async Task SchemaDiff_DestructiveChanges_ReturnSpecificConflict()
    {
        var fixture = await CreateFixtureAsync();
        var initial = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId, twoColumns: true), "blocked-initial");
        var table = initial.Tables.Single();
        var column = table.Columns[0];
        await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId, table.Id, "blocked-row",
            new BatchTableRowsRequest
            {
                SchemaVersion = initial.Version,
                Changes =
                [
                    new TableRowChange
                    {
                        Operation = TableRowOperation.Create, TempId = "row", SortOrder = 0,
                        Cells = new Dictionary<Guid, TableCellRequest>
                        {
                            [column.Id] = new() { Value = "value", IsNull = false }
                        }
                    }
                ]
            });

        var requiredColumn = ExistingSchemaRequest(initial, fixture.PhysicalTypeId,
            includeEmptyColumn: true, newColumnRequired: true);
        var validation = await SchemaBuilderClient.ValidateTargetDbSchemaAsync(fixture.TargetDbId, requiredColumn);
        validation.IsValid.ShouldBeFalse();
        validation.RequiresConfirmation.ShouldBeFalse();
        validation.DestructiveChanges.ShouldContain(x => x.Code == "RequiredColumnNeedsDefault");
        var blocked = await Should.ThrowAsync<ApiException>(() => SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, requiredColumn, "blocked-required"));
        blocked.StatusCode.ShouldBe(409);
        blocked.Problem!.Code.ShouldBe("RequiredColumnNeedsDefault");
        blocked.Problem.Violations.ShouldHaveSingleItem().Path.ShouldContain("columns[new-column]");
        blocked.Problem.Violations.ShouldHaveSingleItem().AffectedRows.ShouldBe(1);

        var deleteFilled = new SchemaUpsertRequest
        {
            Version = initial.Version,
            Tables =
            [
                new SchemaTableDraft
                {
                    Id = table.Id, Name = table.Name, Description = table.Description,
                    SortOrder = table.SortOrder,
                    Columns =
                    [
                        new SchemaColumnDraft
                        {
                            Id = table.Columns[1].Id, Name = table.Columns[1].Name,
                            PhysicalTypeId = table.Columns[1].PhysicalTypeId,
                            IsPrimaryKey = table.Columns[1].IsPrimaryKey,
                            IsRequired = table.Columns[1].IsRequired,
                            SortOrder = table.Columns[1].SortOrder, Parameters = []
                        }
                    ]
                }
            ],
            Relationships = []
        };
        var deleteValidation = await SchemaBuilderClient.ValidateTargetDbSchemaAsync(
            fixture.TargetDbId, deleteFilled);
        deleteValidation.DestructiveChanges.ShouldContain(x => x.Code == "SchemaChangeBlockedByData");
    }

    [Theory(DisplayName = "Schema diff считает строки с NULL или отсутствующей ячейкой")]
    [MemberData(nameof(NullableCellCases))]
    public async Task NullableToRequired_ReturnsExactAffectedRows(string[] values, long expectedAffectedRows)
    {
        var fixture = await CreateFixtureAsync();
        var initial = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId), $"null-count-initial-{Guid.NewGuid()}");
        var table = initial.Tables.Single();
        var column = table.Columns.Single();
        var recordIds = await SeedNullableCellsAsync(table.Id, column.Id, values);
        var request = SetExistingColumnRequired(initial);

        var validation = await SchemaBuilderClient.ValidateTargetDbSchemaAsync(fixture.TargetDbId, request);

        validation.RequiresConfirmation.ShouldBeFalse();
        validation.ConfirmationToken.ShouldBeNull();
        if (expectedAffectedRows > 0)
        {
            validation.IsValid.ShouldBeFalse();
            var change = validation.DestructiveChanges.ShouldHaveSingleItem();
            change.Kind.ShouldBe("Update");
            change.EntityType.ShouldBe("Column");
            change.Code.ShouldBe("ColumnContainsNullValues");
            change.Severity.ShouldBe("Error");
            change.AffectedRows.ShouldBe(expectedAffectedRows);
            change.Path.ShouldBe($"tables[{table.Id}].columns[{column.Id}]");

            var blocked = await Should.ThrowAsync<ApiException>(() =>
                SchemaBuilderClient.ApplyTargetDbSchemaAsync(
                    fixture.TargetDbId, request, $"null-count-apply-{Guid.NewGuid()}"));
            blocked.StatusCode.ShouldBe(409);
            blocked.Problem.ShouldNotBeNull();
            blocked.Problem.Code.ShouldBe(change.Code);
            var violation = blocked.Problem.Violations.ShouldHaveSingleItem();
            violation.Path.ShouldBe(change.Path);
            violation.Code.ShouldBe(change.Code);
            violation.Severity.ShouldBe(change.Severity);
            violation.AffectedRows.ShouldBe(change.AffectedRows);
        }
        else
        {
            validation.IsValid.ShouldBeTrue();
            validation.DestructiveChanges.ShouldBeEmpty();
            var applied = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
                fixture.TargetDbId, request, $"null-count-safe-{Guid.NewGuid()}");
            applied.Version.ShouldBe("2");
            applied.Tables.Single().Id.ShouldBe(table.Id);
            applied.Tables.Single().Columns.Single().Id.ShouldBe(column.Id);
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.DataRecords.CountAsync(x => recordIds.Contains(x.Id))).ShouldBe(recordIds.Count);
        }
    }

    [Fact(DisplayName = "Schema apply пересчитывает affectedRows после изменения данных")]
    public async Task NullableToRequired_ApplyRecalculatesAffectedRows()
    {
        var fixture = await CreateFixtureAsync();
        var initial = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId), "null-recheck-initial");
        var table = initial.Tables.Single();
        var column = table.Columns.Single();
        var recordIds = await SeedNullableCellsAsync(table.Id, column.Id, ["NULL", "MISSING"]);
        var request = SetExistingColumnRequired(initial);

        var validation = await SchemaBuilderClient.ValidateTargetDbSchemaAsync(fixture.TargetDbId, request);
        validation.DestructiveChanges.ShouldHaveSingleItem().AffectedRows.ShouldBe(2);

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var explicitNull = await db.CellValues.SingleAsync(x => x.DataRecordId == recordIds[0]);
            explicitNull.Update("fixed");
            await db.SaveChangesAsync();
        }

        var oneBlocked = await Should.ThrowAsync<ApiException>(() =>
            SchemaBuilderClient.ApplyTargetDbSchemaAsync(fixture.TargetDbId, request, "null-recheck-one"));
        oneBlocked.Problem!.Violations.ShouldHaveSingleItem().AffectedRows.ShouldBe(1);

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CellValues.Add(global::SQLModule.Domain.Schema.CellValue.Create(
                recordIds[1], column.Id, "fixed"));
            await db.SaveChangesAsync();
        }

        var applied = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, request, "null-recheck-all");
        applied.Version.ShouldBe("2");
        applied.Tables.Single().Id.ShouldBe(table.Id);
        applied.Tables.Single().Columns.Single().Id.ShouldBe(column.Id);
    }

    [Fact(DisplayName = "Teacher DDL: validate и idempotent create восстанавливают метаданные")]
    public async Task DdlWorkflow_CreatesTargetDbAndSnapshot()
    {
        var fixture = await CreateFixtureAsync();
        var fake = App.Services.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<FakeSandboxExecutor>();
        fake.OverrideInspection = new InspectedSchema(
            [new InspectedTable("students",
                [new InspectedColumn("name", "text", false, true, 0, null, null, null)])], []);
        try
        {
            var validateRequest = new ValidateTargetDbDdlRequest
            {
                DbmsId = fixture.DbmsId,
                DbName = "ddl_validate",
                DdlScript = "CREATE TABLE students (name text PRIMARY KEY);"
            };
            var validation = await SchemaBuilderClient.ValidateTargetDbDdlAsync(validateRequest);
            validation.DetectedTables.ShouldBe(1);

            var createRequest = new CreateTargetDbFromDdlRequest
            {
                DbmsId = fixture.DbmsId,
                DbName = "ddl_" + Guid.NewGuid(),
                Description = "DDL",
                IsReadOnly = false,
                DdlScript = validateRequest.DdlScript
            };
            var created = await SchemaBuilderClient.CreateTargetDbFromDdlAsync("ddl-key-1", createRequest);
            var replay = await SchemaBuilderClient.CreateTargetDbFromDdlAsync("ddl-key-1", createRequest);
            replay.TargetDbId.ShouldBe(created.TargetDbId);
            created.Schema.Tables.ShouldHaveSingleItem().Columns.ShouldHaveSingleItem().Name.ShouldBe("name");

            await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateTargetDbDdlAsync(
                new ValidateTargetDbDdlRequest
                {
                    DbmsId = fixture.DbmsId,
                    DbName = "unsafe",
                    DdlScript = "DROP TABLE students;"
                }));
        }
        finally
        {
            fake.OverrideInspection = null;
        }
    }

    [Fact(DisplayName = "Teacher schema: GET, validate, apply и повторный GET согласованы")]
    public async Task SchemaWorkflow_ReturnsConsistentVersionedSnapshot()
    {
        var fixture = await CreateFixtureAsync();

        var initial = await SchemaBuilderClient.GetTargetDbSchemaAsync(fixture.TargetDbId);
        initial.ShouldNotBeNull();
        initial.Version.ShouldBe("0");
        initial.State.ShouldBe(SchemaLifecycleState.Draft);
        initial.Tables.ShouldBeEmpty();
        initial.Capabilities.CanEditSchema.ShouldBeTrue();
        initial.Capabilities.CanEditData.ShouldBeFalse();

        var request = SchemaRequest(initial.Version, fixture.PhysicalTypeId);
        (await SchemaBuilderClient.ValidateTargetDbSchemaAsync(fixture.TargetDbId, request)).IsValid.ShouldBeTrue();
        var applied = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(fixture.TargetDbId, request, "schema-key-1");
        var replay = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(fixture.TargetDbId, request, "schema-key-1");

        applied.Version.ShouldBe("1");
        replay.Version.ShouldBe(applied.Version);
        applied.State.ShouldBe(SchemaLifecycleState.Ready);
        applied.Tables.ShouldHaveSingleItem().Columns.ShouldHaveSingleItem();
        applied.Capabilities.CanEditData.ShouldBeTrue();

        var reopened = await SchemaBuilderClient.GetTargetDbSchemaAsync(fixture.TargetDbId);
        reopened.ShouldNotBeNull();
        reopened.Version.ShouldBe(applied.Version);
        reopened.Tables[0].Id.ShouldBe(applied.Tables[0].Id);
        reopened.Tables[0].Columns[0].Id.ShouldBe(applied.Tables[0].Columns[0].Id);

        var stale = await Should.ThrowAsync<ApiException>(
            () => SchemaBuilderClient.ApplyTargetDbSchemaAsync(fixture.TargetDbId, request, "schema-key-stale"));
        stale.StatusCode.ShouldBe(412);
    }

    [Fact(DisplayName = "Schema API передаёт публичные enum строками")]
    public async Task SchemaApi_SerializesPublicEnumsAsStrings()
    {
        var fixture = await CreateFixtureAsync();

        using var draftResponse = await HttpClient.GetAsync(
            ApiRoutes.Schema.TargetDbs.ForSchema(fixture.TargetDbId));
        draftResponse.EnsureSuccessStatusCode();
        using (var draftJson = JsonDocument.Parse(await draftResponse.Content.ReadAsStringAsync()))
        {
            draftJson.RootElement.GetProperty("state").GetString().ShouldBe("Draft");
        }

        var applied = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId));
        var table = applied.Tables.Single();
        var column = table.Columns.Single();

        using var readyResponse = await HttpClient.GetAsync(
            ApiRoutes.Schema.TargetDbs.ForSchema(fixture.TargetDbId));
        readyResponse.EnsureSuccessStatusCode();
        using (var readyJson = JsonDocument.Parse(await readyResponse.Content.ReadAsStringAsync()))
        {
            readyJson.RootElement.GetProperty("state").GetString().ShouldBe("Ready");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            ApiRoutes.Schema.TargetDbs.ForTableRows(fixture.TargetDbId, table.Id))
        {
            Content = new StringContent(
                $$"""
                  {
                    "schemaVersion": "{{applied.Version}}",
                    "changes": [
                      {
                        "operation": "Create",
                        "tempId": "string-enum-row",
                        "sortOrder": 0,
                        "cells": {
                          "{{column.Id}}": { "value": "value", "isNull": false }
                        }
                      }
                    ]
                  }
                  """,
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", "string-enum-row");

        using var rowsResponse = await HttpClient.SendAsync(request);

        rowsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rows = await SchemaBuilderClient.GetTableRowsAsync(fixture.TargetDbId, table.Id, 0, 50);
        rows!.Items.ShouldHaveSingleItem().Cells[column.Id].Value.ShouldBe("value");
    }

    [Fact(DisplayName = "Teacher data: batch различает NULL и пустую строку и идемпотентен")]
    public async Task RowsBatch_IsAtomicAndDistinguishesNullFromEmpty()
    {
        var fixture = await CreateFixtureAsync();
        var applied = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId, twoColumns: true));
        var table = applied.Tables.ShouldHaveSingleItem();
        var first = table.Columns[0].Id;
        var second = table.Columns[1].Id;
        var request = new BatchTableRowsRequest
        {
            SchemaVersion = applied.Version,
            Changes =
            [
                new TableRowChange
                {
                    Operation = TableRowOperation.Create,
                    TempId = "row-1",
                    SortOrder = 0,
                    Cells = new Dictionary<Guid, TableCellRequest>
                    {
                        [first] = new() { Value = "", IsNull = false },
                        [second] = new() { Value = null, IsNull = true }
                    }
                }
            ]
        };

        var saved = await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId, table.Id, "rows-key-1", request);
        var replay = await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId, table.Id, "rows-key-1", request);

        replay.SchemaVersion.ShouldBe(saved.SchemaVersion);
        replay.CreatedIds["row-1"].ShouldBe(saved.CreatedIds["row-1"]);
        replay.Rows.ShouldHaveSingleItem().Id.ShouldBe(saved.Rows.ShouldHaveSingleItem().Id);
        var page = await SchemaBuilderClient.GetTableRowsAsync(fixture.TargetDbId, table.Id, 0, 50);
        page.ShouldNotBeNull();
        page.Count.ShouldBe(1);
        page.Items[0].Cells[first].Value.ShouldBe("");
        page.Items[0].Cells[first].IsNull.ShouldBeFalse();
        page.Items[0].Cells[second].Value.ShouldBeNull();
        page.Items[0].Cells[second].IsNull.ShouldBeTrue();

        await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId,
            table.Id,
            "rows-key-invalid",
            new BatchTableRowsRequest
            {
                SchemaVersion = applied.Version,
                Changes =
                [
                    new TableRowChange
                    {
                        Operation = TableRowOperation.Create,
                        TempId = "bad",
                        SortOrder = 1,
                        Cells = new Dictionary<Guid, TableCellRequest>
                        {
                            [Guid.NewGuid()] = new() { Value = "bad", IsNull = false }
                        }
                    }
                ]
            }));
        (await SchemaBuilderClient.GetTableRowsAsync(fixture.TargetDbId, table.Id, 0, 50))!.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Teacher data: физическая ошибка отклоняет весь batch без раскрытия ошибки СУБД")]
    public async Task RowsBatch_PhysicalConstraintViolation_RollsBackWithSafeError()
    {
        var fixture = await CreateFixtureAsync();
        var applied = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId, SchemaRequest("0", fixture.PhysicalTypeId));
        var table = applied.Tables.ShouldHaveSingleItem();
        var column = table.Columns.ShouldHaveSingleItem();
        var fake = App.Services.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<FakeSandboxExecutor>();
        fake.ResetValidateSetup();
        fake.OverrideSetup = Result.Fail(Error.Validation(
            "Sandbox.SetupFailed",
            "секретный текст ошибки PostgreSQL"));

        try
        {
            var exception = await Should.ThrowAsync<ValidationException>(() =>
                SchemaBuilderClient.SaveTableRowsAsync(
                    fixture.TargetDbId,
                    table.Id,
                    "physical-validation-failed",
                    new BatchTableRowsRequest
                    {
                        SchemaVersion = applied.Version,
                        Changes =
                        [
                            new TableRowChange
                            {
                                Operation = TableRowOperation.Create,
                                TempId = "invalid-row",
                                SortOrder = 0,
                                Cells = new Dictionary<Guid, TableCellRequest>
                                {
                                    [column.Id] = new() { Value = "invalid", IsNull = false }
                                }
                            }
                        ]
                    }));

            exception.Problem!.Code.ShouldBe("TargetDbData.ConstraintViolation");
            exception.Problem.Detail!.ShouldNotContain("PostgreSQL");
            fake.ValidateSetupCallCount.ShouldBe(1);
            fake.LastSetup.ShouldNotBeNull();
            fake.LastSetup.Statements.ShouldContain(x => x.Contains("invalid", StringComparison.Ordinal));
            (await SchemaBuilderClient.GetTableRowsAsync(
                fixture.TargetDbId,
                table.Id,
                0,
                50))!.Count.ShouldBe(0);
        }
        finally
        {
            fake.ResetValidateSetup();
        }
    }

    [Fact(DisplayName = "Teacher data: FK проверяется атомарно с числовой семантикой и допускает NULL")]
    public async Task RowsBatch_ForeignKeyValidation_RejectsMissingAndAcceptsEquivalentNumberAndNull()
    {
        var fixture = await CreateFixtureAsync("INTEGER");
        var schema = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId,
            RelatedTablesSchemaRequest("0", fixture.PhysicalTypeId));
        var parent = schema.Tables.Single(table => table.Name == "courses");
        var child = schema.Tables.Single(table => table.Name == "enrollments");
        var parentId = parent.Columns.Single(column => column.Name == "id");
        var childId = child.Columns.Single(column => column.Name == "id");
        var courseId = child.Columns.Single(column => column.Name == "course_id");

        var missingReference = await Should.ThrowAsync<ValidationException>(() =>
            SchemaBuilderClient.SaveTableRowsAsync(
                fixture.TargetDbId,
                child.Id,
                "missing-course",
                CreateRowsRequest(schema.Version, childId.Id, courseId.Id, "1", "5")));

        missingReference.Problem!.Code.ShouldBe("TargetDbData.ReferenceNotFound");
        missingReference.Problem.Detail!.ShouldContain("course_id");
        missingReference.Problem.Detail!.ShouldContain("courses");
        missingReference.Problem.Detail!.ShouldContain("5");
        (await SchemaBuilderClient.GetTableRowsAsync(
            fixture.TargetDbId, child.Id, 0, 50))!.Count.ShouldBe(0);

        await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId,
            parent.Id,
            "create-course",
            CreateRowsRequest(schema.Version, parentId.Id, null, "5", null));
        await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId,
            child.Id,
            "equivalent-course-id",
            CreateRowsRequest(schema.Version, childId.Id, courseId.Id, "2", "05"));
        await SchemaBuilderClient.SaveTableRowsAsync(
            fixture.TargetDbId,
            child.Id,
            "null-course-id",
            CreateRowsRequest(schema.Version, childId.Id, courseId.Id, "3", null));

        (await SchemaBuilderClient.GetTableRowsAsync(
            fixture.TargetDbId, child.Id, 0, 50))!.Count.ShouldBe(2);
    }

    [Fact(DisplayName = "Teacher lookup: фильтрует, пагинирует и не схлопывает одинаковые значения")]
    public async Task LookupValues_FiltersPaginatesAndKeepsDuplicates()
    {
        var fixture = await CreateFixtureAsync("INTEGER");
        Guid labelTypeId;
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var labelType = PhysicalType.Create(fixture.DbmsId, "TEXT");
            db.PhysicalTypes.Add(labelType);
            await db.SaveChangesAsync();
            labelTypeId = labelType.Id;
        }

        var schema = await SchemaBuilderClient.ApplyTargetDbSchemaAsync(
            fixture.TargetDbId,
            RelatedTablesSchemaRequest("0", fixture.PhysicalTypeId, labelTypeId));
        var courses = schema.Tables.Single(table => table.Name == "courses");
        var enrollments = schema.Tables.Single(table => table.Name == "enrollments");
        var valueColumn = courses.Columns.Single(column => column.Name == "id");
        var labelColumn = courses.Columns.Single(column => column.Name == "name");
        var foreignColumn = enrollments.Columns.Single(column => column.Name == "course_id");

        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AddLookupRow(db, courses.Id, valueColumn.Id, labelColumn.Id, "2", "Дубликат", 0);
            AddLookupRow(db, courses.Id, valueColumn.Id, labelColumn.Id, null, "Без значения", 1);
            AddLookupRow(db, courses.Id, valueColumn.Id, labelColumn.Id, "2", "Базы данных", 2);
            AddLookupRow(db, courses.Id, valueColumn.Id, labelColumn.Id, "3", "Алгоритмы", 3);
            await db.SaveChangesAsync();
        }

        var all = await SchemaBuilderClient.GetLookupValuesAsync(
            fixture.TargetDbId, courses.Id, valueColumn.Id, labelColumn.Id, limit: 2);
        all.Count.ShouldBe(3);
        all.Offset.ShouldBe(0);
        all.Limit.ShouldBe(2);
        all.Items.Count.ShouldBe(2);
        all.Items[0].Label.ShouldBe("2 — Дубликат");
        all.Items[1].Label.ShouldBe("2 — Базы данных");

        var searched = await SchemaBuilderClient.GetLookupValuesAsync(
            fixture.TargetDbId, courses.Id, valueColumn.Id, labelColumn.Id, "БАЗЫ");
        searched.Count.ShouldBe(1);
        searched.Items.ShouldHaveSingleItem().Label.ShouldBe("2 — Базы данных");

        var withoutLabel = await SchemaBuilderClient.GetLookupValuesAsync(
            fixture.TargetDbId, courses.Id, valueColumn.Id, limit: 100);
        withoutLabel.Items.ShouldAllBe(item => item.Label == item.Value);

        var wrongTable = await Should.ThrowAsync<ValidationException>(() =>
            SchemaBuilderClient.GetLookupValuesAsync(
                fixture.TargetDbId, courses.Id, foreignColumn.Id));
        wrongTable.Problem!.Code.ShouldBe("LookupColumnDoesNotBelongToTable");
    }

    [Fact(DisplayName = "Schema FK: целевая колонка обязана быть первичным ключом")]
    public async Task SchemaRelationship_TargetWithoutUniqueConstraint_IsRejected()
    {
        var fixture = await CreateFixtureAsync("INTEGER");

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            SchemaBuilderClient.ApplyTargetDbSchemaAsync(
                fixture.TargetDbId,
                RelatedTablesSchemaRequest("0", fixture.PhysicalTypeId, targetPrimaryKey: false)));

        exception.Problem!.Detail!.ShouldContain("первичный ключ");
    }

    [Fact(DisplayName = "Schema FK: типы исходной и целевой колонок должны совпадать")]
    public async Task SchemaRelationship_IncompatibleColumnTypes_AreRejected()
    {
        var fixture = await CreateFixtureAsync("INTEGER");
        Guid incompatibleTypeId;
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var incompatibleType = PhysicalType.Create(fixture.DbmsId, "TEXT");
            db.PhysicalTypes.Add(incompatibleType);
            await db.SaveChangesAsync();
            incompatibleTypeId = incompatibleType.Id;
        }

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            SchemaBuilderClient.ApplyTargetDbSchemaAsync(
                fixture.TargetDbId,
                RelatedTablesSchemaRequest(
                    "0",
                    fixture.PhysicalTypeId,
                    foreignPhysicalTypeId: incompatibleTypeId)));

        exception.Problem!.Detail!.ShouldContain("несовместимы");
    }

    private async Task<IReadOnlyList<Guid>> SeedNullableCellsAsync(
        Guid tableId,
        Guid columnId,
        IReadOnlyList<string> values)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var recordIds = new List<Guid>();
        for (var index = 0; index < values.Count; index++)
        {
            var record = global::SQLModule.Domain.Schema.DataRecord.Create(tableId, index);
            recordIds.Add(record.Id);
            db.DataRecords.Add(record);
            if (values[index] != "MISSING")
            {
                db.CellValues.Add(global::SQLModule.Domain.Schema.CellValue.Create(
                    record.Id,
                    columnId,
                    values[index] == "NULL" ? null : values[index]));
            }
        }

        await db.SaveChangesAsync();
        return recordIds;
    }

    private static void AddLookupRow(
        AppDbContext db,
        Guid tableId,
        Guid valueColumnId,
        Guid labelColumnId,
        string? value,
        string label,
        int sortOrder)
    {
        var record = global::SQLModule.Domain.Schema.DataRecord.Create(tableId, sortOrder);
        db.DataRecords.Add(record);
        db.CellValues.Add(global::SQLModule.Domain.Schema.CellValue.Create(
            record.Id, valueColumnId, value));
        db.CellValues.Add(global::SQLModule.Domain.Schema.CellValue.Create(
            record.Id, labelColumnId, label));
    }

    private static SchemaUpsertRequest SetExistingColumnRequired(TargetDbSchemaResponse current)
    {
        var table = current.Tables.Single();
        var column = table.Columns.Single();
        return new SchemaUpsertRequest
        {
            Version = current.Version,
            Tables =
            [
                new SchemaTableDraft
                {
                    Id = table.Id,
                    Name = table.Name,
                    Description = table.Description,
                    SortOrder = table.SortOrder,
                    Columns =
                    [
                        new SchemaColumnDraft
                        {
                            Id = column.Id,
                            Name = column.Name,
                            PhysicalTypeId = column.PhysicalTypeId,
                            IsPrimaryKey = column.IsPrimaryKey,
                            IsRequired = true,
                            SortOrder = column.SortOrder,
                            Parameters = column.Parameters.Select(parameter =>
                                new SchemaColumnParameterDraft
                                {
                                    ParameterDefinitionId = parameter.ParameterDefinitionId,
                                    Value = parameter.Value
                                }).ToList()
                        }
                    ]
                }
            ],
            Relationships = []
        };
    }

    private async Task<(Guid TargetDbId, Guid PhysicalTypeId, Guid DbmsId)> CreateFixtureAsync(
        string physicalTypeName = "text")
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbms = DbmsDictionary.Create(
            "Schema_" + Guid.NewGuid(), "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "testdb", "user", "pass");
        var physicalType = PhysicalType.Create(dbms.Id, physicalTypeName);
        var target = Domain.Schema.TargetDb.Create(dbms.Id, "Target_" + Guid.NewGuid(), null, false);
        db.AddRange(dbms, physicalType, target);
        await db.SaveChangesAsync();
        return (target.Id, physicalType.Id, dbms.Id);
    }

    private static SchemaUpsertRequest SchemaRequest(string version, Guid physicalTypeId, bool twoColumns = false)
    {
        var columns = new List<SchemaColumnDraft>
        {
            new()
            {
                TempId = "column-1", Name = "value", PhysicalTypeId = physicalTypeId,
                IsPrimaryKey = false, IsRequired = false, SortOrder = 0, Parameters = []
            }
        };
        if (twoColumns)
        {
            columns.Add(new SchemaColumnDraft
            {
                TempId = "column-2",
                Name = "optional",
                PhysicalTypeId = physicalTypeId,
                IsPrimaryKey = false,
                IsRequired = false,
                SortOrder = 1,
                Parameters = []
            });
        }

        return new SchemaUpsertRequest
        {
            Version = version,
            Tables =
            [
                new SchemaTableDraft
                {
                    TempId = "table-1", Name = "sample", Description = "Sample",
                    SortOrder = 0, Columns = columns
                }
            ],
            Relationships = []
        };
    }

    private static SchemaUpsertRequest RelatedTablesSchemaRequest(
        string version,
        Guid physicalTypeId,
        Guid? labelPhysicalTypeId = null,
        bool targetPrimaryKey = true,
        Guid? foreignPhysicalTypeId = null) => new()
    {
        Version = version,
        Tables =
        [
            new SchemaTableDraft
            {
                TempId = "courses", Name = "courses", SortOrder = 0,
                Columns =
                [
                    new SchemaColumnDraft
                    {
                        TempId = "course-id", Name = "id", PhysicalTypeId = physicalTypeId,
                        IsPrimaryKey = targetPrimaryKey, IsRequired = true, SortOrder = 0, Parameters = []
                    },
                    new SchemaColumnDraft
                    {
                        TempId = "course-name", Name = "name",
                        PhysicalTypeId = labelPhysicalTypeId ?? physicalTypeId,
                        IsPrimaryKey = false, IsRequired = false, SortOrder = 1, Parameters = []
                    }
                ]
            },
            new SchemaTableDraft
            {
                TempId = "enrollments", Name = "enrollments", SortOrder = 1,
                Columns =
                [
                    new SchemaColumnDraft
                    {
                        TempId = "enrollment-id", Name = "id", PhysicalTypeId = physicalTypeId,
                        IsPrimaryKey = true, IsRequired = true, SortOrder = 0, Parameters = []
                    },
                    new SchemaColumnDraft
                    {
                        TempId = "enrollment-course-id", Name = "course_id",
                        PhysicalTypeId = foreignPhysicalTypeId ?? physicalTypeId,
                        IsPrimaryKey = false, IsRequired = false, SortOrder = 1, Parameters = []
                    }
                ]
            }
        ],
        Relationships =
        [
            new SchemaRelationshipDraft
            {
                TempId = "fk-enrollments-courses",
                Name = "fk_enrollments_courses",
                SourceColumnRef = "enrollment-course-id",
                TargetColumnRef = "course-id"
            }
        ]
    };

    private static BatchTableRowsRequest CreateRowsRequest(
        string schemaVersion,
        Guid idColumnId,
        Guid? referenceColumnId,
        string id,
        string? reference)
    {
        var cells = new Dictionary<Guid, TableCellRequest>
        {
            [idColumnId] = new() { Value = id, IsNull = false }
        };
        if (referenceColumnId.HasValue)
        {
            cells[referenceColumnId.Value] = new()
            {
                Value = reference,
                IsNull = reference is null
            };
        }

        return new BatchTableRowsRequest
        {
            SchemaVersion = schemaVersion,
            Changes =
            [
                new TableRowChange
                {
                    Operation = TableRowOperation.Create,
                    TempId = $"row-{id}",
                    SortOrder = 0,
                    Cells = cells
                }
            ]
        };
    }

    private static SchemaUpsertRequest ExistingSchemaRequest(
        TargetDbSchemaResponse current,
        Guid physicalTypeId,
        bool includeEmptyColumn,
        bool? newColumnRequired,
        bool addTable = false)
    {
        var table = current.Tables.Single(x => x.Name == "sample");
        var columns = table.Columns
            .Where((_, index) => includeEmptyColumn || index == 0)
            .Select(column => new SchemaColumnDraft
            {
                Id = column.Id,
                Name = column.Name,
                PhysicalTypeId = column.PhysicalTypeId,
                IsPrimaryKey = column.IsPrimaryKey,
                IsRequired = column.IsRequired,
                SortOrder = column.SortOrder,
                Parameters = column.Parameters.Select(x =>
                    new SchemaColumnParameterDraft
                    {
                        ParameterDefinitionId = x.ParameterDefinitionId,
                        Value = x.Value
                    }).ToList()
            }).ToList();
        if (newColumnRequired.HasValue)
        {
            columns.Add(new SchemaColumnDraft
            {
                TempId = "new-column",
                Name = "added",
                PhysicalTypeId = physicalTypeId,
                IsPrimaryKey = false,
                IsRequired = newColumnRequired.Value,
                SortOrder = checked((short)columns.Count),
                Parameters = []
            });
        }

        var tables = new List<SchemaTableDraft>
        {
            new()
            {
                Id = table.Id, Name = table.Name, Description = "Updated description",
                SortOrder = 5, Columns = columns
            }
        };
        if (addTable)
        {
            tables.Add(new SchemaTableDraft
            {
                TempId = "new-table",
                Name = "added_table",
                Description = null,
                SortOrder = 6,
                Columns =
                [
                    new SchemaColumnDraft
                    {
                        TempId = "new-table-column", Name = "id", PhysicalTypeId = physicalTypeId,
                        IsPrimaryKey = true, IsRequired = true, SortOrder = 0, Parameters = []
                    }
                ]
            });
        }

        return new SchemaUpsertRequest
        {
            Version = current.Version,
            Tables = tables,
            Relationships = current.Relationships.Select(x => new SchemaRelationshipDraft
            {
                Id = x.Id,
                Name = x.Name,
                SourceColumnRef = x.SourceColumnId.ToString(),
                TargetColumnRef = x.TargetColumnId.ToString(),
                DeleteRule = x.DeleteRule,
                UpdateRule = x.UpdateRule
            }).ToList()
        };
    }
}
