using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal record CreateCellValueCommand(Guid DataRecordId, Guid MetaAttributeId, string? TextValue)
    : IRequest<Result<CellValueResponse>>;

internal sealed class CreateCellValueHandler(AppDbContext db)
    : IRequestHandler<CreateCellValueCommand, Result<CellValueResponse>>
{
    public async Task<Result<CellValueResponse>> Handle(CreateCellValueCommand command, CancellationToken ct)
    {
        if (!await db.DataRecords.AnyAsync(x => x.Id == command.DataRecordId, ct))
        {
            return Result<CellValueResponse>.Fail(CellValueErrors.DataRecordNotFound(command.DataRecordId));
        }

        if (!await db.MetaAttributes.AnyAsync(x => x.Id == command.MetaAttributeId, ct))
        {
            return Result<CellValueResponse>.Fail(CellValueErrors.MetaAttributeNotFound(command.MetaAttributeId));
        }

        var entity = CellValue.Create(command.DataRecordId, command.MetaAttributeId, command.TextValue);
        db.CellValues.Add(entity);
        await db.SaveChangesAsync(ct);
        return CellValueMappings.ToResponse(entity);
    }
}
