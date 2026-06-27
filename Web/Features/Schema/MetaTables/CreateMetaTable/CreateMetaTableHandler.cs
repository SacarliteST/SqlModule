using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaTables;

internal record CreateMetaTableCommand(Guid TargetDbId, string TableName, string? Description)
    : IRequest<Result<MetaTableResponse>>;

internal sealed class CreateMetaTableHandler(AppDbContext db)
    : IRequestHandler<CreateMetaTableCommand, Result<MetaTableResponse>>
{
    public async Task<Result<MetaTableResponse>> Handle(CreateMetaTableCommand command, CancellationToken ct)
    {
        if (!await db.TargetDbs.AnyAsync(x => x.Id == command.TargetDbId, ct))
        {
            return Result<MetaTableResponse>.Fail(MetaTableErrors.TargetDbNotFound(command.TargetDbId));
        }

        var entity = MetaTable.Create(command.TargetDbId, command.TableName, command.Description);
        db.MetaTables.Add(entity);
        await db.SaveChangesAsync(ct);
        return MetaTableMappings.ToResponse(entity);
    }
}
