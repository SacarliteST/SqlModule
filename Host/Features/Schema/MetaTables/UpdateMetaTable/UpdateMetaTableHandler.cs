using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaTables;

internal record UpdateMetaTableCommand(Guid Id, string TableName, string? Description)
    : IRequest<Result>;

internal sealed class UpdateMetaTableHandler(AppDbContext db)
    : IRequestHandler<UpdateMetaTableCommand, Result>
{
    public async Task<Result> Handle(UpdateMetaTableCommand command, CancellationToken ct)
    {
        var entity = await db.MetaTables.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(MetaTableErrors.NotFound(command.Id));
        }

        entity.Update(command.TableName, command.Description);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
