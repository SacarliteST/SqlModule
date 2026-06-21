namespace SQLModule.Sandbox;

/// <summary>Значение одной ячейки строки данных.</summary>
/// <param name="ColumnKey">Ключ колонки (соответствует <see cref="ColumnSpec.Key"/>).</param>
/// <param name="Value">Значение ячейки; <c>null</c> означает SQL NULL.</param>
public sealed record CellSpec(string ColumnKey, string? Value);
