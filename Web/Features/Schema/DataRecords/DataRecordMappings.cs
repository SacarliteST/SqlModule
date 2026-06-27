using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.DataRecords;

internal static class DataRecordMappings
{
    internal static DataRecordResponse ToResponse(DataRecord e) => new(
        e.Id, e.MetaTableId, e.SortOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateDataRecordCommand ToCommand(CreateDataRecordRequest req) =>
        new(req.MetaTableId, req.SortOrder);

    internal static UpdateDataRecordCommand ToCommand(Guid id, UpdateDataRecordRequest req) =>
        new(id, req.SortOrder);
}
