using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaRelationships;

internal record DeleteMetaRelationshipCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteMetaRelationshipHandler(AppDbContext db)
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
