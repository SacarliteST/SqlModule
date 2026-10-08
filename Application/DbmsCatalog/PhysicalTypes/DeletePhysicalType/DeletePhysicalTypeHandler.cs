using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public record DeletePhysicalTypeCommand(Guid Id) : IRequest<Result>;

public sealed class DeletePhysicalTypeHandler(IApplicationDbContext db)
    : IRequestHandler<DeletePhysicalTypeCommand, Result>
{
    public async Task<Result> Handle(DeletePhysicalTypeCommand command, CancellationToken ct)
    {
        var entity = await db.PhysicalTypes.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(PhysicalTypeErrors.NotFound(command.Id));
        }

        if (await db.MetaAttributes.AnyAsync(a => a.PhysicalTypeId == command.Id, ct))
        {
            return Result.Fail(PhysicalTypeErrors.InUse);
        }

        db.PhysicalTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
