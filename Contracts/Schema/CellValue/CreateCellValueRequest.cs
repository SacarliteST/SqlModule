namespace SQLModule.Contracts.Schema.CellValue;

/// <summary>Запрос на создание значения ячейки (EAV).</summary>
/// <param name="DataRecordId">Идентификатор строки данных (EAV-якоря), которой принадлежит ячейка.</param>
/// <param name="MetaAttributeId">Идентификатор мета-атрибута (столбца).</param>
/// <param name="TextValue">Текстовое значение ячейки (null — если значение не задано; до 2000 символов).</param>
public record CreateCellValueRequest(Guid DataRecordId, Guid MetaAttributeId, string? TextValue);
