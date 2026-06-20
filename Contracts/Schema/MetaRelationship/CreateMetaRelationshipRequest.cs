namespace SQLModule.Contracts.Schema.MetaRelationship;

/// <summary>Запрос на создание связи между двумя мета-атрибутами.</summary>
/// <param name="RelationshipName">Название связи (FK). Обязательно, до 200 символов.</param>
/// <param name="SourceAttributeId">Id исходного мета-атрибута (сторона «многие»).</param>
/// <param name="TargetAttributeId">Id целевого мета-атрибута (сторона «один»). Должен отличаться от SourceAttributeId.</param>
/// <param name="DeleteRule">Правило при удалении (CASCADE, RESTRICT, SET NULL, SET DEFAULT, NO ACTION). До 50 символов.</param>
/// <param name="UpdateRule">Правило при обновлении. До 50 символов.</param>
public record CreateMetaRelationshipRequest(
    string RelationshipName,
    Guid SourceAttributeId,
    Guid TargetAttributeId,
    string? DeleteRule,
    string? UpdateRule);
