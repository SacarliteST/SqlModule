using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.TargetDbs;

public record UpdateTargetDbCommand(Guid Id, string DbName, string? Description, bool IsReadOnly)
    : IRequest<Result>;

public sealed class UpdateTargetDbHandler(IApplicationDbContext db)
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
