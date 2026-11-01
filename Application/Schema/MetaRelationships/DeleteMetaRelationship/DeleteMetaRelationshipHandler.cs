using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.MetaRelationships;

public record DeleteMetaRelationshipCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteMetaRelationshipHandler(IApplicationDbContext db)
    : IRequestHandler<DeleteMetaRelationshipCommand, Result>
{
    public async Task<Result> Handle(DeleteMetaRelationshipCommand command, CancellationToken ct)
    {
        var entity = await db.MetaRelationships.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(MetaRelationshipErrors.NotFound(command.Id));
        }

        db.MetaRelationships.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
