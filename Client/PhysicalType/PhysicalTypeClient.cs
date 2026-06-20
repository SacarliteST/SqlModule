using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;

namespace SQLModule.Client.PhysicalType;

internal sealed class PhysicalTypeClient(HttpClient httpClient)
    : CrudClientBase<CreatePhysicalTypeRequest, UpdatePhysicalTypeRequest, PhysicalTypeResponse>(httpClient),
        IPhysicalTypeClient
{
    protected override string Collection => ApiRoutes.DbmsCatalog.PhysicalTypes.Collection;
    protected override string ForId(Guid id) => ApiRoutes.DbmsCatalog.PhysicalTypes.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.DbmsCatalog.PhysicalTypes.ForPagination(offset, limit);
}
