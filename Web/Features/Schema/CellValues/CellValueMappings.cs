using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.CellValues;

internal static class CellValueMappings
{
    internal static CellValueResponse ToResponse(CellValue e) => new(
        e.Id, e.DataRecordId, e.MetaAttributeId, e.TextValue,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateCellValueCommand ToCommand(CreateCellValueRequest r) =>
        new(r.DataRecordId, r.MetaAttributeId, r.TextValue);
}
