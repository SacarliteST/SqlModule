using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public static class ParameterDefinitionErrors
{
    public static Error NotFound(Guid id) =>
        DomainErrors<ParameterDefinition>.NotFound(id);

    public static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<ParameterDefinition>.Conflict($"Физический тип '{id}' не найден.");

    public static Error AlreadyExists =>
        DomainErrors<ParameterDefinition>.Conflict("Параметр с таким ключом уже задан для этого типа.");
}
