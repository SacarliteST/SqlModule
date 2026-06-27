using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

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
