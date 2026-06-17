using SQLModule.Domain.Common;

namespace SQLModule.Domain.Schema;

/// <summary>Описание связи (Foreign Key) между двумя мета-атрибутами.</summary>
public sealed class MetaRelationship : AuditableEntity
{
    public string RelationshipName { get; private set; }
    public Guid SourceAttributeId { get; private set; }
    public Guid TargetAttributeId { get; private set; }
    public string? DeleteRule { get; private set; }
    public string? UpdateRule { get; private set; }

    public MetaAttribute SourceAttribute { get; private set; } = default!;
    public MetaAttribute TargetAttribute { get; private set; } = default!;

    private MetaRelationship(Guid id, string relationshipName,
        Guid sourceAttributeId, Guid targetAttributeId,
        string? deleteRule, string? updateRule) : base(id)
    {
        RelationshipName = relationshipName;
        SourceAttributeId = sourceAttributeId;
        TargetAttributeId = targetAttributeId;
        DeleteRule = deleteRule;
        UpdateRule = updateRule;
    }

    public static MetaRelationship Create(
        string relationshipName,
        Guid sourceAttributeId, Guid targetAttributeId,
        string? deleteRule, string? updateRule,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), relationshipName, sourceAttributeId, targetAttributeId, deleteRule, updateRule);

    public void Update(string relationshipName, string? deleteRule, string? updateRule)
    {
        RelationshipName = relationshipName;
        DeleteRule = deleteRule;
        UpdateRule = updateRule;
    }
}
