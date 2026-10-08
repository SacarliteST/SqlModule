using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaRelationships;

public static class MetaRelationshipErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<MetaRelationship>.NotFound(id);

    public static Error SourceAttributeNotFound(Guid id) =>
        DomainErrors<MetaRelationship>.Conflict($"Исходная колонка '{id}' не найдена.");

    public static Error TargetAttributeNotFound(Guid id) =>
        DomainErrors<MetaRelationship>.Conflict($"Целевая колонка '{id}' не найдена.");
}
