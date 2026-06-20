using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;

namespace SQLModule.Client.MetaTable;

internal sealed class MetaTableClient(HttpClient httpClient)
    : CrudClientBase<CreateMetaTableRequest, UpdateMetaTableRequest, MetaTableResponse>(httpClient), IMetaTableClient
{
    protected override string Collection => ApiRoutes.Schema.MetaTables.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.MetaTables.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.MetaTables.ForPagination(offset, limit);
}
