using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public static class AttributeParameterValueErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<AttributeParameterValue>.NotFound(id);

    public static Error MetaAttributeNotFound(Guid id) =>
        DomainErrors<AttributeParameterValue>.Conflict($"Колонка '{id}' не найдена.");

    public static Error ParameterDefinitionNotFound(Guid id) =>
        DomainErrors<AttributeParameterValue>.Conflict($"Определение параметра '{id}' не найдено.");

    public static Error AlreadyExists =>
        DomainErrors<AttributeParameterValue>.Conflict("Значение этого параметра для колонки уже задано.");
}
