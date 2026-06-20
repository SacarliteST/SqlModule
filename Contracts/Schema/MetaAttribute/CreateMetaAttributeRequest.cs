namespace SQLModule.Contracts.Schema.MetaAttribute;

/// <summary>Запрос на создание мета-атрибута (колонки) таблицы.</summary>
/// <param name="MetaTableId">Идентификатор мета-таблицы, которой принадлежит колонка.</param>
/// <param name="PhysicalTypeId">Идентификатор физического типа данных колонки.</param>
/// <param name="AttributeName">Название колонки (не более 200 символов).</param>
/// <param name="IsPrimaryKey">Признак первичного ключа.</param>
/// <param name="IsRequired">Признак обязательности значения.</param>
/// <param name="SortOrder">Порядок отображения колонки (≥ 0).</param>
public record CreateMetaAttributeRequest(
    Guid MetaTableId,
    Guid PhysicalTypeId,
    string AttributeName,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder);
