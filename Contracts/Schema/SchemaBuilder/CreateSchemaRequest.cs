namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Запрос на построение схемы в песочнице (с временными идентификаторами).</summary>
/// <param name="DbmsId">Идентификатор СУБД из справочника.</param>
/// <param name="SchemaName">Название схемы (или базы данных).</param>
/// <param name="Tables">Список таблиц-черновиков.</param>
/// <param name="Relationships">Список связей между колонками (Foreign Key).</param>
public record CreateSchemaRequest(
    Guid DbmsId,
    string SchemaName,
    IReadOnlyList<TableDraft> Tables,
    IReadOnlyList<RelationshipDraft> Relationships);
