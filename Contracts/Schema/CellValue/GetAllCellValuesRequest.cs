namespace SQLModule.Contracts.Schema.CellValue;

/// <summary>Параметры запроса постраничного списка значений ячеек.</summary>
/// <param name="Offset">Смещение от начала списка (≥ 0).</param>
/// <param name="Limit">Количество записей на странице (1–100).</param>
public record GetAllCellValuesRequest(int Offset = 0, int Limit = 20);
