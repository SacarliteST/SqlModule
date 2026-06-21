using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal record DeleteMetaAttributeCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteMetaAttributeHandler(AppDbContext db)
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
