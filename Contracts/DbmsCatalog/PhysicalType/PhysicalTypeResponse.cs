namespace SQLModule.Contracts.DbmsCatalog.PhysicalType;

/// <summary>Ответ с данными физического типа данных.</summary>
/// <param name="Id">Идентификатор физического типа данных.</param>
/// <param name="DbmsId">Идентификатор СУБД, которой принадлежит тип.</param>
/// <param name="TypeName">Название физического типа данных.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи.</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним обновившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления записи.</param>
public record PhysicalTypeResponse(
    Guid Id,
    Guid DbmsId,
    string TypeName,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
