using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

internal record DeletePhysicalTypeCommand(Guid Id) : IRequest<Result>;

internal sealed class DeletePhysicalTypeHandler(AppDbContext db)
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
