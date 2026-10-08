using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.SchemaBuilder;

public static class SchemaAggregateErrors
{
    public static Error NotFound(Guid id) => DomainErrors<TargetDb>.NotFound(id);
    public static Error EditingForbidden(Guid id) =>
        DomainErrors<TargetDb>.Conflict($"Схему базы '{id}' нельзя редактировать: база доступна только для чтения.");
    public static Error VersionConflict(string expected, string actual) =>
        Error.PreconditionFailed("SchemaVersionConflict", $"Версия схемы устарела: ожидалась '{expected}', текущая '{actual}'.");
    public static Error HasData =>
        new("DestructiveSchemaChangeRequiresConfirmation", "Изменение схемы с учебными данными требует отдельного подтверждения.", ErrorType.Conflict);
}
