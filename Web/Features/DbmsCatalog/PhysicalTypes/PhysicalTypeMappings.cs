using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

internal static class PhysicalTypeMappings
{
    internal static PhysicalTypeResponse ToResponse(PhysicalType e) => new(
        e.Id, e.DbmsId, e.TypeName,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreatePhysicalTypeCommand ToCommand(CreatePhysicalTypeRequest req) =>
        new(req.DbmsId, req.TypeName);

    internal static UpdatePhysicalTypeCommand ToCommand(Guid id, UpdatePhysicalTypeRequest req) =>
        new(id, req.TypeName);
}
