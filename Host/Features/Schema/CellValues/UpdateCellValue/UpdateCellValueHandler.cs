using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal record UpdateCellValueCommand(Guid Id, string? TextValue) : IRequest<Result<CellValueResponse>>;

internal sealed class UpdateCellValueHandler(AppDbContext db)
    : IRequestHandler<UpdateCellValueCommand, Result<CellValueResponse>>
{
    public async Task<Result<CellValueResponse>> Handle(UpdateCellValueCommand command, CancellationToken ct)
    {
        var entity = await db.CellValues.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result<CellValueResponse>.Fail(CellValueErrors.NotFound(command.Id));
        }

        entity.Update(command.TextValue);
        await db.SaveChangesAsync(ct);
        return CellValueMappings.ToResponse(entity);
    }
}
