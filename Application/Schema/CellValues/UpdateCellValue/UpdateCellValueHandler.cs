using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.CellValues;

public record UpdateCellValueCommand(Guid Id, string? TextValue) : IRequest<Result<CellValueResponse>>;

public sealed class UpdateCellValueHandler(IApplicationDbContext db)
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
