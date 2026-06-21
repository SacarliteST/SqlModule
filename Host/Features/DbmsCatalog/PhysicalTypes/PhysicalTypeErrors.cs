using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal static class PhysicalTypeErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<PhysicalType>.NotFound(id);

    internal static Error DbmsNotFound(Guid id) =>
        DomainErrors<PhysicalType>.Conflict($"СУБД '{id}' не найдена.");

    internal static Error InUse =>
        DomainErrors<PhysicalType>.Conflict("Физический тип используется атрибутами и не может быть удалён.");
}
