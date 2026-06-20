using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;

namespace SQLModule.Client.AttributeParameterValue;

internal sealed class AttributeParameterValueClient(HttpClient httpClient)
    : CrudClientBase<CreateAttributeParameterValueRequest, UpdateAttributeParameterValueRequest,
        AttributeParameterValueResponse>(httpClient),
        IAttributeParameterValueClient
{
    protected override string Collection => ApiRoutes.Schema.AttributeParameterValues.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.AttributeParameterValues.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.AttributeParameterValues.ForPagination(offset, limit);
}
