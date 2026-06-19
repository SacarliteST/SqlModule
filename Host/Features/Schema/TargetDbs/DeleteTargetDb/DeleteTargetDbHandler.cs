using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal record DeleteTargetDbCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteTargetDbHandler(AppDbContext db)
    : IRequestHandler<DeleteTargetDbCommand, Result>
{
    public async Task<Result> Handle(DeleteTargetDbCommand command, CancellationToken ct)
    {
        var entity = await db.TargetDbs.FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (entity is null)
        {
            return Result.Fail(Error.NotFound(nameof(TargetDb), command.Id));
        }

        db.TargetDbs.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
