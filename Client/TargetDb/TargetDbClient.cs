using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;

namespace SQLModule.Client.TargetDb;

internal sealed class TargetDbClient(HttpClient httpClient)
    : CrudClientBase<CreateTargetDbRequest, UpdateTargetDbRequest, TargetDbResponse>(httpClient), ITargetDbClient
{
    protected override string Collection => ApiRoutes.Schema.TargetDbs.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.TargetDbs.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.TargetDbs.ForPagination(offset, limit);
}
