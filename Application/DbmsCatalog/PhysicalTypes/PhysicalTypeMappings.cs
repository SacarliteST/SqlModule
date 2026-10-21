using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public static class PhysicalTypeMappings
{
    public static PhysicalTypeResponse ToResponse(PhysicalType e) => new(
        e.Id, e.DbmsId, e.TypeName,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreatePhysicalTypeCommand ToCommand(CreatePhysicalTypeRequest req) =>
        new(req.DbmsId, req.TypeName);

    public static UpdatePhysicalTypeCommand ToCommand(Guid id, UpdatePhysicalTypeRequest req) =>
        new(id, req.TypeName);
}
