using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public record UpdatePhysicalTypeCommand(Guid Id, string TypeName) : IRequest<Result>;

public sealed class UpdatePhysicalTypeHandler(IApplicationDbContext db)
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
