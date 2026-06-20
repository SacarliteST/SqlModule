namespace SQLModule.Contracts.Schema.DataRecord;

/// <summary>Параметры запроса постраничного списка строк данных.</summary>
/// <param name="Offset">Смещение от начала списка (≥ 0).</param>
/// <param name="Limit">Количество записей на странице (1–100).</param>
/// <param name="MetaTableId">Опциональный фильтр по мета-таблице — вернуть только строки указанной таблицы.</param>
public record GetAllDataRecordsRequest(int Offset = 0, int Limit = 20, Guid? MetaTableId = null);
