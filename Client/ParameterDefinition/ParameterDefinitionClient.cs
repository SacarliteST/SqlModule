using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

namespace SQLModule.Client.ParameterDefinition;

internal sealed class ParameterDefinitionClient(HttpClient httpClient)
    : CrudClientBase<CreateParameterDefinitionRequest, UpdateParameterDefinitionRequest, ParameterDefinitionResponse>(httpClient),
        IParameterDefinitionClient
{
    protected override string Collection => ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection;
    protected override string ForId(Guid id) => ApiRoutes.DbmsCatalog.ParameterDefinitions.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.DbmsCatalog.ParameterDefinitions.ForPagination(offset, limit);
}
