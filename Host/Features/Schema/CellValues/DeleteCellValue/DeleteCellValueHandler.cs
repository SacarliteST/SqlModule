using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal record DeleteCellValueCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteCellValueHandler(AppDbContext db)
    : IRequestHandler<DeleteCellValueCommand, Result>
{
    public async Task<Result> Handle(DeleteCellValueCommand command, CancellationToken ct)
    {
        var entity = await db.CellValues.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(CellValueErrors.NotFound(command.Id));
        }

        db.CellValues.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
