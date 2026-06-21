using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaTables;

internal record DeleteMetaTableCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteMetaTableHandler(AppDbContext db)
    : IRequestHandler<DeleteMetaTableCommand, Result>
{
    public async Task<Result> Handle(DeleteMetaTableCommand command, CancellationToken ct)
    {
        var entity = await db.MetaTables.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(MetaTableErrors.NotFound(command.Id));
        }

        var attrIds = db.MetaAttributes
            .Where(a => a.MetaTableId == command.Id)
            .Select(a => a.Id);

        if (await db.MetaRelationships.AnyAsync(
                r => attrIds.Contains(r.SourceAttributeId) || attrIds.Contains(r.TargetAttributeId), ct))
        {
            return Result.Fail(MetaTableErrors.InUse);
        }

        db.MetaTables.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
