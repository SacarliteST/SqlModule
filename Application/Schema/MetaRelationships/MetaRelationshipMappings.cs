using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaRelationships;

public static class MetaRelationshipMappings
{
    public static MetaRelationshipResponse ToResponse(MetaRelationship e) => new(
        e.Id, e.RelationshipName, e.SourceAttributeId, e.TargetAttributeId,
        e.DeleteRule, e.UpdateRule,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateMetaRelationshipCommand ToCommand(CreateMetaRelationshipRequest req) =>
        new(req.RelationshipName, req.SourceAttributeId, req.TargetAttributeId,
            req.DeleteRule, req.UpdateRule);

    public static UpdateMetaRelationshipCommand ToCommand(Guid id, UpdateMetaRelationshipRequest req) =>
        new(id, req.RelationshipName, req.DeleteRule, req.UpdateRule);
}
