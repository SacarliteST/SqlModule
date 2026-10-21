using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.CellValues;

public static class CellValueMappings
{
    public static CellValueResponse ToResponse(CellValue e) => new(
        e.Id, e.DataRecordId, e.MetaAttributeId, e.TextValue,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateCellValueCommand ToCommand(CreateCellValueRequest r) =>
        new(r.DataRecordId, r.MetaAttributeId, r.TextValue);
}
