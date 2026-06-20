namespace SQLModule.Contracts.Schema.MetaTable;

/// <summary>Ответ с данными мета-таблицы.</summary>
/// <param name="Id">Идентификатор мета-таблицы.</param>
/// <param name="TargetDbId">Идентификатор целевой БД.</param>
/// <param name="TableName">Название таблицы.</param>
/// <param name="Description">Описание таблицы.</param>
/// <param name="CreatedById">Идентификатор создавшего пользователя.</param>
/// <param name="CreatedAt">Дата и время создания.</param>
/// <param name="UpdatedById">Идентификатор последнего редактора.</param>
/// <param name="UpdatedAt">Дата и время последнего обновления.</param>
public record MetaTableResponse(
    Guid Id,
    Guid TargetDbId,
    string TableName,
    string? Description,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
