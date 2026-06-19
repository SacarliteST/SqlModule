namespace SQLModule.Contracts.Training.SqlQuery;

/// <summary>Ответ с данными эталонного SQL-запроса.</summary>
/// <param name="Id">Уникальный идентификатор запроса.</param>
/// <param name="QueryText">Текст SQL-запроса.</param>
/// <param name="StrictColumnOrder">Признак обязательного порядка колонок.</param>
/// <param name="StrictRowOrder">Признак обязательного порядка строк.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения записи.</param>
public record SqlQueryResponse(
    Guid Id,
    string QueryText,
    bool StrictColumnOrder,
    bool StrictRowOrder,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
