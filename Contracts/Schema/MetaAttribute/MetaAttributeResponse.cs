namespace SQLModule.Contracts.Schema.MetaAttribute;

/// <summary>Ответ с данными мета-атрибута (колонки) таблицы.</summary>
/// <param name="Id">Идентификатор мета-атрибута.</param>
/// <param name="MetaTableId">Идентификатор мета-таблицы.</param>
/// <param name="PhysicalTypeId">Идентификатор физического типа данных.</param>
/// <param name="AttributeName">Название колонки.</param>
/// <param name="IsPrimaryKey">Признак первичного ключа.</param>
/// <param name="IsRequired">Признак обязательности значения.</param>
/// <param name="SortOrder">Порядок отображения колонки.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним обновившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления записи.</param>
public record MetaAttributeResponse(
    Guid Id,
    Guid MetaTableId,
    Guid PhysicalTypeId,
    string AttributeName,
    bool IsPrimaryKey,
    bool IsRequired,
    short SortOrder,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
