using SQLModule.Common.Results;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal static class ParameterDefinitionErrors
{
    internal static Error NotFound(Guid id) =>
        DomainErrors<ParameterDefinition>.NotFound(id);

    internal static Error PhysicalTypeNotFound(Guid id) =>
        DomainErrors<ParameterDefinition>.Conflict($"Физический тип '{id}' не найден.");

    internal static Error AlreadyExists =>
        DomainErrors<ParameterDefinition>.Conflict("Параметр с таким ключом уже задан для этого типа.");
}
