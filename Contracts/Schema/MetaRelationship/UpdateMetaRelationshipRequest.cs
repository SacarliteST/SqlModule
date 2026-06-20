namespace SQLModule.Contracts.Schema.MetaRelationship;

/// <summary>Запрос на обновление связи между мета-атрибутами. FK-колонки не изменяются.</summary>
/// <param name="RelationshipName">Новое название связи. Обязательно, до 200 символов.</param>
/// <param name="DeleteRule">Правило при удалении. До 50 символов.</param>
/// <param name="UpdateRule">Правило при обновлении. До 50 символов.</param>
public record UpdateMetaRelationshipRequest(
    string RelationshipName,
    string? DeleteRule,
    string? UpdateRule);
