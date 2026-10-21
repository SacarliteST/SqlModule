using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaAttributes;

public record DeleteMetaAttributeCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteMetaAttributeHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteMetaAttributeCommand, Result>
{
    public async Task<Result> Handle(DeleteMetaAttributeCommand command, CancellationToken ct)
    {
        var entity = await db.MetaAttributes.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(MetaAttributeErrors.NotFound(command.Id));
        }

        if (await db.MetaRelationships.AnyAsync(
                r => r.SourceAttributeId == command.Id || r.TargetAttributeId == command.Id, ct))
        {
            return Result.Fail(MetaAttributeErrors.InUse);
        }

        db.MetaAttributes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
