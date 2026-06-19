using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal record UpdateTargetDbCommand(Guid Id, string DbName, string? Description, bool IsReadOnly)
    : IRequest<Result>;

internal sealed class UpdateTargetDbHandler(AppDbContext db)
    : IRequestHandler<UpdateTargetDbCommand, Result>
{
    public async Task<Result> Handle(UpdateTargetDbCommand command, CancellationToken ct)
    {
        var entity = await db.TargetDbs.FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (entity is null)
        {
            return Result.Fail(Error.NotFound(nameof(TargetDb), command.Id));
        }

        entity.Update(command.DbName, command.Description, command.IsReadOnly);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
