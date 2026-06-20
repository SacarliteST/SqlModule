namespace SQLModule.Contracts.Schema.MetaRelationship;

/// <summary>Ответ с данными связи между мета-атрибутами.</summary>
/// <param name="Id">Идентификатор связи.</param>
/// <param name="RelationshipName">Название связи (FK).</param>
/// <param name="SourceAttributeId">Id исходного мета-атрибута.</param>
/// <param name="TargetAttributeId">Id целевого мета-атрибута.</param>
/// <param name="DeleteRule">Правило при удалении.</param>
/// <param name="UpdateRule">Правило при обновлении.</param>
/// <param name="CreatedById">Id пользователя, создавшего запись.</param>
/// <param name="CreatedAt">Дата и время создания.</param>
/// <param name="UpdatedById">Id пользователя, последним изменившего запись.</param>
/// <param name="UpdatedAt">Дата и время последнего изменения.</param>
public record MetaRelationshipResponse(
    Guid Id,
    string RelationshipName,
    Guid SourceAttributeId,
    Guid TargetAttributeId,
    string? DeleteRule,
    string? UpdateRule,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
