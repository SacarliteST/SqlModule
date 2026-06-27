using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.AttributeParameterValues;

internal static class AttributeParameterValueErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<AttributeParameterValue>.NotFound(id);

    internal static Error MetaAttributeNotFound(Guid id) =>
        DomainErrors<AttributeParameterValue>.Conflict($"Колонка '{id}' не найдена.");

    internal static Error ParameterDefinitionNotFound(Guid id) =>
        DomainErrors<AttributeParameterValue>.Conflict($"Определение параметра '{id}' не найдено.");

    internal static Error AlreadyExists =>
        DomainErrors<AttributeParameterValue>.Conflict("Значение этого параметра для колонки уже задано.");
}
