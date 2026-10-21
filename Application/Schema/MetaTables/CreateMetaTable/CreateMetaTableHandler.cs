using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Application.Persistence;
using SQLModule.Domain.Schema;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaTables;

public record CreateMetaTableCommand(Guid TargetDbId, string TableName, string? Description)
    : IRequest<Result<MetaTableResponse>>;

public sealed class CreateMetaTableHandler(IApplicationDbContext db)
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
