using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal record CreateTargetDbCommand(Guid DbmsId, string DbName, string? Description, bool IsReadOnly)
    : IRequest<Result<TargetDbResponse>>;

internal sealed class CreateTargetDbHandler(AppDbContext db)
    : IRequestHandler<CreateTargetDbCommand, Result<TargetDbResponse>>
{
    public async Task<Result<TargetDbResponse>> Handle(CreateTargetDbCommand command, CancellationToken ct)
    {
        if (!await db.DbmsDictionaries.AnyAsync(x => x.Id == command.DbmsId, ct))
        {
            return Result<TargetDbResponse>.Fail(Error.Conflict(
                "TargetDb.DbmsNotFound",
                $"СУБД с id '{command.DbmsId}' не найдена."));
        }

        var entity = TargetDb.Create(command.DbmsId, command.DbName, command.Description, command.IsReadOnly);
        db.TargetDbs.Add(entity);
        await db.SaveChangesAsync(ct);
        return TargetDbMappings.ToResponse(entity);
    }
}
