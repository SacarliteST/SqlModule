namespace SQLModule.Sandbox;

/// <summary>Нейтральное описание схемы БД для генерации SQL (не зависит от доменных сущностей).</summary>
/// <param name="SchemaName">Название схемы (или базы данных).</param>
/// <param name="Tables">Список таблиц.</param>
/// <param name="Relationships">Список связей (Foreign Key).</param>
public sealed record SchemaSpec(
    string SchemaName,
    IReadOnlyList<TableSpec> Tables,
    IReadOnlyList<RelationshipSpec> Relationships);
