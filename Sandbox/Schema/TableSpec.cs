namespace SQLModule.Sandbox;

/// <summary>Описание таблицы в нейтральной модели схемы.</summary>
/// <param name="Key">Строковый ключ таблицы (tempId из запроса или Guid из сохранённых данных).</param>
/// <param name="Name">Имя таблицы в целевой БД.</param>
/// <param name="Columns">Список колонок, упорядоченных по SortOrder.</param>
public sealed record TableSpec(
    string Key,
    string Name,
    IReadOnlyList<ColumnSpec> Columns);
