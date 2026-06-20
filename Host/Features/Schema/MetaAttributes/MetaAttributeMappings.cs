using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Domain.Schema;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal static class MetaAttributeMappings
{
    internal static MetaAttributeResponse ToResponse(MetaAttribute e) => new(
        e.Id, e.MetaTableId, e.PhysicalTypeId, e.AttributeName,
        e.IsPrimaryKey, e.IsRequired, e.SortOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateMetaAttributeCommand ToCommand(CreateMetaAttributeRequest req) =>
        new(req.MetaTableId, req.PhysicalTypeId, req.AttributeName,
            req.IsPrimaryKey, req.IsRequired, req.SortOrder);

    internal static UpdateMetaAttributeCommand ToCommand(Guid id, UpdateMetaAttributeRequest req) =>
        new(id, req.AttributeName, req.IsPrimaryKey, req.IsRequired, req.SortOrder);
}
