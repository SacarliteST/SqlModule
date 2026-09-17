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

    [Fact(DisplayName = "D5.1: дублирующийся TempId колонки → ValidationException (422)")]
    public async Task ValidateAsync_DuplicateColumnTempId_Throws422()
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
        ex.StatusCode.ShouldBe(422);
        ex.Errors.ShouldContainKey("Tables");
    }

    // ── D5.2: связь ссылается на несуществующий TempId → 400 ────────────────

    [Fact(DisplayName = "D5.2: SourceColumnTempId не существует → ValidationException (422)")]
    public async Task ValidateAsync_RelationshipWithMissingSourceTempId_Throws422()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "a", [new ColumnDraft("c1", "id", AnyPhysicalTypeId, true, true, 0, [])])],
            [new RelationshipDraft("fk", "nonexistent_src", "c1", null, null)]);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(422);
        ex.Errors.ShouldContainKey("Relationships");
    }

    // ── D5.3: Source == Target → 400 ────────────────────────────────────────

    [Fact(DisplayName = "D5.3: SourceColumnTempId == TargetColumnTempId → ValidationException (422)")]
    public async Task ValidateAsync_SelfReferenceRelationship_Throws422()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "s",
            [new TableDraft("t1", "a", [new ColumnDraft("c1", "id", AnyPhysicalTypeId, true, true, 0, [])])],
            [new RelationshipDraft("fk_self", "c1", "c1", null, null)]);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(422);
        ex.Errors.ShouldContainKey("Relationships");
    }

    [Fact(DisplayName = "D5.3a: одна исходная колонка не может иметь два внешних ключа")]
    public async Task ValidateAsync_DuplicateOutgoingRelationship_Throws422()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(),
            "s",
            [
                new TableDraft("t1", "a",
                [
                    new ColumnDraft("source", "target_id", AnyPhysicalTypeId, false, true, 0, []),
                    new ColumnDraft("target-1", "id_1", AnyPhysicalTypeId, true, true, 1, []),
                    new ColumnDraft("target-2", "id_2", AnyPhysicalTypeId, true, true, 2, [])
                ])
            ],
            [
                new RelationshipDraft("fk_1", "source", "target-1", null, null),
                new RelationshipDraft("fk_2", "source", "target-2", null, null)
            ]);

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            SchemaBuilderClient.ValidateAsync(request));

        exception.StatusCode.ShouldBe(422);
        exception.Errors["Relationships"].ShouldContain(message =>
            message.Contains("только одну внешнюю связь", StringComparison.Ordinal));
    }

    // ── D5.4: пустые Tables → 400 ───────────────────────────────────────────

    [Fact(DisplayName = "D5.4: пустой список таблиц → ValidationException (422)")]
    public async Task ValidateAsync_EmptyTables_Throws422()
    {
        var request = new CreateSchemaRequest(Guid.NewGuid(), "s", [], []);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(422);
    }

    // ── D5.5: пустой SchemaName → 400 ───────────────────────────────────────

    [Fact(DisplayName = "D5.5: пустое имя схемы → ValidationException (422)")]
    public async Task ValidateAsync_EmptySchemaName_Throws422()
    {
        var request = new CreateSchemaRequest(
            Guid.NewGuid(), "",
            [new TableDraft("t1", "a", [new ColumnDraft("c1", "id", AnyPhysicalTypeId, true, true, 0, [])])],
            []);

        var ex = await Should.ThrowAsync<ValidationException>(() => SchemaBuilderClient.ValidateAsync(request));
        ex.StatusCode.ShouldBe(422);
    }
}
