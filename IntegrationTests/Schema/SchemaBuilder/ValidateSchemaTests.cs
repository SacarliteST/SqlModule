using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.SchemaBuilder;

/// <summary>
/// Структурные тесты валидации черновика схемы (FluentValidation, без Docker).
/// Используют основной прогон с FakeSandboxExecutor.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ValidateSchemaTests : ApiTestBase
{
    private static readonly Guid AnyPhysicalTypeId = Guid.NewGuid();

    public ValidateSchemaTests(TestApplication testApplication) : base(testApplication) { }

    // ── D5.1: дубликат TempId колонок → 400 ─────────────────────────────────

    [Fact(DisplayName = "D5.1: дублирующийся TempId колонки → ValidationException (400)")]
    public async Task ValidateAsync_DuplicateColumnTempId_Throws400()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [
                new TableDraft("t1", "a", [
                    new ColumnDraft("c_dup", "id", AnyPhysicalTypeId, true, true, 0, []),
                    new ColumnDraft("c_dup", "name", AnyPhysicalTypeId, false, true, 1, [])
                ])
            ],
            []);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(400);
        ex.Errors.ShouldContainKey("Tables");
    }

    // ── D5.2: связь ссылается на несуществующий TempId → 400 ────────────────

    [Fact(DisplayName = "D5.2: SourceColumnTempId не существует → ValidationException (400)")]
    public async Task ValidateAsync_RelationshipWithMissingSourceTempId_Throws400()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "a", [new ColumnDraft("c1", "id", AnyPhysicalTypeId, true, true, 0, [])])],
            [new RelationshipDraft("fk", "nonexistent_src", "c1", null, null)]);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(400);
        ex.Errors.ShouldContainKey("Relationships");
    }

    // ── D5.3: Source == Target → 400 ────────────────────────────────────────

    [Fact(DisplayName = "D5.3: SourceColumnTempId == TargetColumnTempId → ValidationException (400)")]
    public async Task ValidateAsync_SelfReferenceRelationship_Throws400()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "a", [new ColumnDraft("c1", "id", AnyPhysicalTypeId, true, true, 0, [])])],
            [new RelationshipDraft("fk_self", "c1", "c1", null, null)]);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(400);
        ex.Errors.ShouldContainKey("Relationships");
    }

    // ── D5.4: пустые Tables → 400 ───────────────────────────────────────────

    [Fact(DisplayName = "D5.4: пустой список таблиц → ValidationException (400)")]
    public async Task ValidateAsync_EmptyTables_Throws400()
    {
        var request = new CreateSchemaRequest(Guid.NewGuid(), "s", [], []);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(400);
    }

    // ── D5.5: пустой SchemaName → 400 ───────────────────────────────────────

    [Fact(DisplayName = "D5.5: пустое имя схемы → ValidationException (400)")]
    public async Task ValidateAsync_EmptySchemaName_Throws400()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "",
            [new TableDraft("t1", "a", [new ColumnDraft("c1", "id", AnyPhysicalTypeId, true, true, 0, [])])],
            []);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(400);
    }
}
