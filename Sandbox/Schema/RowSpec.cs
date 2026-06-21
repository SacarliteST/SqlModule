namespace SQLModule.Sandbox;

/// <summary>Одна строка данных.</summary>
/// <param name="Cells">Значения ячеек.</param>
public sealed record RowSpec(IReadOnlyList<CellSpec> Cells);
