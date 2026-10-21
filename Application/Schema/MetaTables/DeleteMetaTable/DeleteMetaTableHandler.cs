using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaTables;

public record DeleteMetaTableCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteMetaTableHandler(IApplicationDbContext db)
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
