namespace SQLModule.Contracts.Schema.SchemaBuilder;

/// <summary>Черновик таблицы с временным идентификатором.</summary>
/// <param name="TempId">Временный строковый идентификатор (уникальный внутри запроса).</param>
/// <param name="Name">Имя таблицы в целевой БД.</param>
/// <param name="Columns">Список колонок таблицы.</param>
public record TableDraft(
    string TempId,
    string Name,
    IReadOnlyList<ColumnDraft> Columns);
