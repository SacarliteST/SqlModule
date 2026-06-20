namespace SQLModule.Contracts.Schema.DataRecord;

/// <summary>Ответ с данными строки (EAV-якорь).</summary>
/// <param name="Id">Идентификатор строки данных.</param>
/// <param name="MetaTableId">Идентификатор мета-таблицы, которой принадлежит строка.</param>
/// <param name="SortOrder">Порядок отображения строки (null, если не задан).</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним обновившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления записи.</param>
public record DataRecordResponse(
    Guid Id,
    Guid MetaTableId,
    int? SortOrder,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
