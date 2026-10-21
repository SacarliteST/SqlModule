using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public static class PhysicalTypeErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<PhysicalType>.NotFound(id);

    public static Error DbmsNotFound(Guid id) =>
        DomainErrors<PhysicalType>.Conflict($"СУБД '{id}' не найдена.");

    public static Error InUse =>
        DomainErrors<PhysicalType>.Conflict("Физический тип используется атрибутами и не может быть удалён.");
}
