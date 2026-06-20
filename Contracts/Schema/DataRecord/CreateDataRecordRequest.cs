namespace SQLModule.Contracts.Schema.DataRecord;

/// <summary>Запрос на создание строки данных (EAV-якорь).</summary>
/// <param name="MetaTableId">Идентификатор мета-таблицы, которой принадлежит строка.</param>
/// <param name="SortOrder">Опциональный порядок отображения строки (≥ 0).</param>
public record CreateDataRecordRequest(Guid MetaTableId, int? SortOrder);
