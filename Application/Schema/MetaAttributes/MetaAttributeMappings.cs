using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaAttributes;

public static class MetaAttributeMappings
{
    public static MetaAttributeResponse ToResponse(MetaAttribute e) => new(
        e.Id, e.MetaTableId, e.PhysicalTypeId, e.AttributeName,
        e.IsPrimaryKey, e.IsRequired, e.SortOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateMetaAttributeCommand ToCommand(CreateMetaAttributeRequest req) =>
        new(req.MetaTableId, req.PhysicalTypeId, req.AttributeName,
            req.IsPrimaryKey, req.IsRequired, req.SortOrder);

    public static UpdateMetaAttributeCommand ToCommand(Guid id, UpdateMetaAttributeRequest req) =>
        new(id, req.AttributeName, req.IsPrimaryKey, req.IsRequired, req.SortOrder);
}
