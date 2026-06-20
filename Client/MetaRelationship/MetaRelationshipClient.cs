using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;

namespace SQLModule.Client.MetaRelationship;

internal sealed class MetaRelationshipClient(HttpClient httpClient)
    : CrudClientBase<CreateMetaRelationshipRequest, UpdateMetaRelationshipRequest, MetaRelationshipResponse>(httpClient),
        IMetaRelationshipClient
{
    protected override string Collection => ApiRoutes.Schema.MetaRelationships.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.MetaRelationships.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.MetaRelationships.ForPagination(offset, limit);
}
