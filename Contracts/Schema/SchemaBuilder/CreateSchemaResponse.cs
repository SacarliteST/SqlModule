namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Ответ на успешное создание схемы.</summary>
/// <param name="TargetDbId">Id созданной целевой БД.</param>
/// <param name="Tables">Карта временных Id таблиц → реальных Id + карта колонок.</param>
public record CreateSchemaResponse(Guid TargetDbId, IReadOnlyList<SchemaTableMap> Tables);

/// <summary>Карта таблицы: TempId → реальный MetaTable.Id.</summary>
/// <param name="TempId">Временный Id таблицы из черновика.</param>
/// <param name="Id">Реальный Id созданной MetaTable.</param>
/// <param name="Columns">Карты атрибутов таблицы.</param>
public record SchemaTableMap(string TempId, Guid Id, IReadOnlyList<SchemaColumnMap> Columns);

/// <summary>Карта атрибута: TempId → реальный MetaAttribute.Id.</summary>
/// <param name="TempId">Временный Id колонки из черновика.</param>
/// <param name="Id">Реальный Id созданного MetaAttribute.</param>
public record SchemaColumnMap(string TempId, Guid Id);
