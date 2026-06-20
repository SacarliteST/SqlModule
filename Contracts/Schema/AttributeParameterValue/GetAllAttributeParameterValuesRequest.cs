namespace SQLModule.Contracts.Schema.AttributeParameterValue;

/// <summary>Параметры запроса постраничного списка значений параметров атрибутов.</summary>
/// <param name="Offset">Смещение от начала списка (≥ 0).</param>
/// <param name="Limit">Количество записей на странице (1–100).</param>
/// <param name="MetaAttributeId">Опциональный фильтр по мета-атрибуту — вернуть только значения параметров указанной колонки.</param>
public record GetAllAttributeParameterValuesRequest(
    int Offset = 0,
    int Limit = 20,
    Guid? MetaAttributeId = null);
