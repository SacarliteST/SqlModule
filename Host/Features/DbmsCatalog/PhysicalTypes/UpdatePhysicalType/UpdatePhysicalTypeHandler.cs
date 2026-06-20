using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal record UpdatePhysicalTypeCommand(Guid Id, string TypeName) : IRequest<Result>;

internal sealed class UpdatePhysicalTypeHandler(AppDbContext db)
    : IRequestHandler<UpdatePhysicalTypeCommand, Result>
{
    public async Task<Result> Handle(UpdatePhysicalTypeCommand command, CancellationToken ct)
    {
        var entity = await db.PhysicalTypes.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(PhysicalTypeErrors.NotFound(command.Id));
        }

        entity.Update(command.TypeName);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
