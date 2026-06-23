namespace SQLModule.Contracts.Schema.CellValue;

/// <summary>Ответ с данными значения ячейки (EAV).</summary>
/// <param name="Id">Идентификатор значения ячейки.</param>
/// <param name="DataRecordId">Идентификатор строки данных, которой принадлежит ячейка.</param>
/// <param name="MetaAttributeId">Идентификатор мета-атрибута (столбца), которому соответствует значение.</param>
/// <param name="TextValue">Текстовое значение ячейки (null, если не задано).</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним обновившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления записи.</param>
public record CellValueResponse(
    Guid Id,
    Guid DataRecordId,
    Guid MetaAttributeId,
    string? TextValue,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
