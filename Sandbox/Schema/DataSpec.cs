namespace SQLModule.Sandbox;

/// <summary>Набор строк данных по таблицам для генерации INSERT-инструкций.</summary>
/// <param name="Tables">Строки данных, сгруппированные по ключу таблицы.</param>
public sealed record DataSpec(IReadOnlyList<TableRows> Tables);
