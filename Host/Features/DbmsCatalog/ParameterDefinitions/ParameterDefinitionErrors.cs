using SQLModule.Domain.DbmsCatalog;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

internal static class ParameterDefinitionErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<ParameterDefinition>.NotFound(id);

    internal static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<ParameterDefinition>.Conflict($"Физический тип '{id}' не найден.");

    internal static Error AlreadyExists =>
        DomainErrors<ParameterDefinition>.Conflict("Параметр с таким ключом уже задан для этого типа.");
}
