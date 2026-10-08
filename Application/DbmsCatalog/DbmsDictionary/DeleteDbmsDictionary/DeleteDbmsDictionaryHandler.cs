using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary.DeleteDbmsDictionary;

public record DeleteDbmsDictionaryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteDbmsDictionaryHandler(IApplicationDbContext db)
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
