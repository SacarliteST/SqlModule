namespace SQLModule.Contracts.Schema.MetaAttribute;

/// <summary>Параметры запроса постраничного списка мета-атрибутов.</summary>
/// <param name="Offset">Смещение от начала списка (≥ 0).</param>
/// <param name="Limit">Количество записей на странице (1–100).</param>
/// <param name="MetaTableId">Опциональный фильтр по мета-таблице — вернуть только колонки указанной таблицы.</param>
public record GetAllMetaAttributesRequest(
    int Offset = 0,
    int Limit = 20,
    Guid? MetaTableId = null);
