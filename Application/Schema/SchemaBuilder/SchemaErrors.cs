using SQLModule.Common.Results;
using DomainDbms = SQLModule.Domain.DbmsCatalog.DbmsDictionary;
using DomainPhysicalType = SQLModule.Domain.DbmsCatalog.PhysicalType;
using DomainTargetDb = SQLModule.Domain.Schema.TargetDb;

namespace SQLModule.Application.Schema.SchemaBuilder;

public static class SchemaErrors
{
    public static Error DbmsNotFound(Guid id) =>
        DomainErrors<DomainDbms>.Conflict($"СУБД с id '{id}' не найдена.");

    public static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<DomainPhysicalType>.Conflict($"Физический тип с id '{id}' не найден.");

    public static Error PhysicalTypeMismatch(Guid typeId, string dbmsName) =>
        DomainErrors<DomainPhysicalType>.Validation($"Физический тип '{typeId}' принадлежит другой СУБД (ожидалась '{dbmsName}').");

    public static Error UnsupportedDbms(string name) =>
        DomainErrors<DomainTargetDb>.Validation($"Диалект SQL для СУБД '{name}' не поддерживается.");

    public static Error AlreadyExists(string name) =>
        DomainErrors<DomainTargetDb>.Conflict($"Схема с именем '{name}' уже существует для этой СУБД.");

    public static Error InvalidTypeParameter(string message) =>
        Error.Validation("InvalidTypeParameter", message);

    public static Error InvalidRelationship(string message) =>
        Error.Validation("InvalidRelationship", message);

    public static Error SchemaLimitExceeded =>
        Error.Validation("SchemaValidationFailed", "Превышен допустимый размер схемы.");
}
