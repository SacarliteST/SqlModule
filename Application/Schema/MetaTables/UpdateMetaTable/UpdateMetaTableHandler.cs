using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.MetaTables;

public record UpdateMetaTableCommand(Guid Id, string TableName, string? Description)
    : IRequest<Result>;

public sealed class UpdateMetaTableHandler(IApplicationDbContext db)
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
