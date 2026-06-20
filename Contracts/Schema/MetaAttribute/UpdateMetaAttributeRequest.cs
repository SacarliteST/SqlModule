namespace SQLModule.Contracts.Schema.MetaAttribute;

/// <summary>Запрос на обновление мета-атрибута (колонки) таблицы.</summary>
/// <param name="AttributeName">Новое название колонки (не более 200 символов).</param>
/// <param name="IsPrimaryKey">Признак первичного ключа.</param>
/// <param name="IsRequired">Признак обязательности значения.</param>
/// <param name="SortOrder">Порядок отображения колонки (≥ 0).</param>
public record UpdateMetaAttributeRequest(
    string AttributeName,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder);
