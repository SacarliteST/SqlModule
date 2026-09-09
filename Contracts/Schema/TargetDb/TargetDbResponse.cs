namespace SQLModule.Contracts.Schema.TargetDb;

/// <summary>Данные целевой БД-песочницы.</summary>
/// <param name="Id">Уникальный идентификатор записи.</param>
/// <param name="DbmsId">Идентификатор СУБД из справочника.</param>
/// <param name="DbName">Имя БД-песочницы.</param>
/// <param name="Description">Описание назначения БД (может быть <c>null</c>).</param>
/// <param name="IsReadOnly">Признак доступа только на чтение.</param>
/// <param name="CreatedById">Идентификатор пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания записи (UTC).</param>
/// <param name="UpdatedById">Идентификатор пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения записи (UTC).</param>
/// <param name="CreatedByName">Отображаемое имя автора на момент создания.</param>
/// <param name="UpdatedByName">Отображаемое имя последнего редактора.</param>
public record TargetDbResponse(
    Guid Id,
    Guid DbmsId,
    string DbName,
    string? Description,
    bool IsReadOnly,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt,
    string? CreatedByName = null,
    string? UpdatedByName = null);
