using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.DataRecords;

public static class DataRecordMappings
{
    public static DataRecordResponse ToResponse(DataRecord e) => new(
        e.Id, e.MetaTableId, e.SortOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateDataRecordCommand ToCommand(CreateDataRecordRequest req) =>
        new(req.MetaTableId, req.SortOrder);

    public static UpdateDataRecordCommand ToCommand(Guid id, UpdateDataRecordRequest req) =>
        new(id, req.SortOrder);
}
