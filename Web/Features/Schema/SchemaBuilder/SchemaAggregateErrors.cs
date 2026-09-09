using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal static class SchemaAggregateErrors
{
    internal static Error NotFound(Guid id) => DomainErrors<TargetDb>.NotFound(id);
    internal static Error EditingForbidden(Guid id) =>
        DomainErrors<TargetDb>.Conflict($"Схему базы '{id}' нельзя редактировать: база доступна только для чтения.");
    internal static Error VersionConflict(string expected, string actual) =>
        Error.PreconditionFailed("SchemaVersionConflict", $"Версия схемы устарела: ожидалась '{expected}', текущая '{actual}'.");
    internal static Error HasData =>
        new("DestructiveSchemaChangeRequiresConfirmation", "Изменение схемы с учебными данными требует отдельного подтверждения.", ErrorType.Conflict);
}
