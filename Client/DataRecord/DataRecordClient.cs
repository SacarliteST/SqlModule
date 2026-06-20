using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;

namespace SQLModule.Client.DataRecord;

internal sealed class DataRecordClient(HttpClient httpClient)
    : CrudClientBase<CreateDataRecordRequest, UpdateDataRecordRequest, DataRecordResponse>(httpClient),
        IDataRecordClient
{
    protected override string Collection => ApiRoutes.Schema.DataRecords.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Schema.DataRecords.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Schema.DataRecords.ForPagination(offset, limit);
}
