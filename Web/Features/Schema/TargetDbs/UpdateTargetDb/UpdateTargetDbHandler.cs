using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.TargetDbs;

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
            return Result.Fail(TargetDbErrors.NotFound(command.Id));
        }

        entity.Update(command.DbName, command.Description, command.IsReadOnly);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
