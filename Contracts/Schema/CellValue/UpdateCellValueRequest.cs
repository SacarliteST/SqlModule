namespace SQLModule.Contracts.Schema.CellValue;

/// <summary>Запрос на обновление значения ячейки (EAV).</summary>
/// <param name="TextValue">Новое текстовое значение ячейки (null — сброс; до 2000 символов).</param>
public record UpdateCellValueRequest(string? TextValue);
