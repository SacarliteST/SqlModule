using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal static class MetaRelationshipErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<MetaRelationship>.NotFound(id);

    internal static Error SourceAttributeNotFound(Guid id) =>
        DomainErrors<MetaRelationship>.Conflict($"Исходная колонка '{id}' не найдена.");

    internal static Error TargetAttributeNotFound(Guid id) =>
        DomainErrors<MetaRelationship>.Conflict($"Целевая колонка '{id}' не найдена.");
}
