using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.TargetDbs;

public record CreateTargetDbCommand(Guid DbmsId, string DbName, string? Description, bool IsReadOnly)
    : IRequest<Result<TargetDbResponse>>;

public sealed class CreateTargetDbHandler(IApplicationDbContext db)
    : IRequestHandler<CreateTargetDbCommand, Result<TargetDbResponse>>
{
    public async Task<Result<TargetDbResponse>> Handle(CreateTargetDbCommand command, CancellationToken ct)
    {
        if (!await db.DbmsDictionaries.AnyAsync(x => x.Id == command.DbmsId, ct))
        {
            return Result<TargetDbResponse>.Fail(TargetDbErrors.DbmsNotFound(command.DbmsId));
        }

        var entity = TargetDb.Create(command.DbmsId, command.DbName, command.Description, command.IsReadOnly);
        db.TargetDbs.Add(entity);
        await db.SaveChangesAsync(ct);
        return TargetDbMappings.ToResponse(entity);
    }
}
