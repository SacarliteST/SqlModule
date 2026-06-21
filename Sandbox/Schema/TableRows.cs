namespace SQLModule.Sandbox;

/// <summary>Строки данных одной таблицы.</summary>
/// <param name="TableKey">Ключ таблицы (соответствует <see cref="TableSpec.Key"/>).</param>
/// <param name="Rows">Список строк.</param>
public sealed record TableRows(string TableKey, IReadOnlyList<RowSpec> Rows);
