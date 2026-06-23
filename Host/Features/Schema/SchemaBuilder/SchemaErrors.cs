using SQLModule.Common.Results;
using DomainDbms = SQLModule.Domain.DbmsCatalog.DbmsDictionary;
using DomainPhysicalType = SQLModule.Domain.DbmsCatalog.PhysicalType;
using DomainTargetDb = SQLModule.Domain.Schema.TargetDb;

namespace SQLModule.Host.Features.Schema.SchemaBuilder;

internal static class SchemaErrors
{
    internal static Error DbmsNotFound(Guid id) =>
        DomainErrors<DomainDbms>.Conflict($"СУБД с id '{id}' не найдена.");

    internal static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<DomainPhysicalType>.Conflict($"Физический тип с id '{id}' не найден.");

    internal static Error PhysicalTypeMismatch(Guid typeId, string dbmsName) =>
        DomainErrors<DomainPhysicalType>.Validation($"Физический тип '{typeId}' принадлежит другой СУБД (ожидалась '{dbmsName}').");

    internal static Error UnsupportedDbms(string name) =>
        DomainErrors<DomainTargetDb>.Validation($"Диалект SQL для СУБД '{name}' не поддерживается.");

    internal static Error AlreadyExists(string name) =>
        DomainErrors<DomainTargetDb>.Conflict($"Схема с именем '{name}' уже существует для этой СУБД.");
}
