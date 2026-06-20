namespace SQLModule.Contracts.Schema.DataRecord;

/// <summary>Запрос на обновление строки данных.</summary>
/// <param name="SortOrder">Новый порядок отображения строки (≥ 0). Передайте null для сброса.</param>
public record UpdateDataRecordRequest(int? SortOrder);
