namespace SQLModule.Sandbox;

/// <summary>Описание колонки в нейтральной модели схемы.</summary>
/// <param name="Key">Строковый ключ колонки (tempId или Guid).</param>
/// <param name="Name">Имя колонки в целевой БД.</param>
/// <param name="SqlType">Готовая строка SQL-типа (например, VARCHAR(255), INTEGER). Вычисляется до передачи генератору.</param>
/// <param name="IsPrimaryKey">Признак первичного ключа.</param>
/// <param name="IsNullable">Если <c>true</c> — колонка допускает NULL.</param>
/// <param name="SortOrder">Порядок в таблице.</param>
public sealed record ColumnSpec(
    string Key,
    string Name,
    string SqlType,
    bool IsPrimaryKey,
    bool IsNullable,
    short SortOrder);
