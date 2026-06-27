using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.DeleteDbmsDictionary;

internal record DeleteDbmsDictionaryCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteDbmsDictionaryHandler(AppDbContext db)
    : IRequestHandler<DeleteDbmsDictionaryCommand, Result>
{
    public async Task<Result> Handle(DeleteDbmsDictionaryCommand command, CancellationToken ct)
    {
        var entity = await db.DbmsDictionaries.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(DbmsDictionaryErrors.NotFound(command.Id));
        }

        if (await db.TargetDbs.AnyAsync(t => t.DbmsId == command.Id, ct) ||
            await db.PhysicalTypes.AnyAsync(p => p.DbmsId == command.Id, ct))
        {
            return Result.Fail(DbmsDictionaryErrors.InUse);
        }

        db.DbmsDictionaries.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
