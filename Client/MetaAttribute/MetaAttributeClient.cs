using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;

namespace SQLModule.Client.MetaAttribute;

internal sealed class MetaAttributeClient(HttpClient httpClient)
    : CrudClientBase<CreateMetaAttributeRequest, UpdateMetaAttributeRequest, MetaAttributeResponse>(httpClient),
        IMetaAttributeClient
{
    protected override string Collection => ApiRoutes.Schema.MetaAttributes.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.MetaAttributes.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.MetaAttributes.ForPagination(offset, limit);
}
