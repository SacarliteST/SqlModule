using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.CellValues;

public record DeleteCellValueCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCellValueHandler(IApplicationDbContext db)
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
