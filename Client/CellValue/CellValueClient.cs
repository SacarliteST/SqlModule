using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;

namespace SQLModule.Client.CellValue;

internal sealed class CellValueClient(HttpClient httpClient)
    : CrudClientBase<CreateCellValueRequest, UpdateCellValueRequest, CellValueResponse>(httpClient),
        ICellValueClient
{
    protected override string Collection => ApiRoutes.Schema.CellValues.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.CellValues.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.CellValues.ForPagination(offset, limit);
}
