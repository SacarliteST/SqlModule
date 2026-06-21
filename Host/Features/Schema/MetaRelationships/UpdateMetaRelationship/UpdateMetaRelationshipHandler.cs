using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal record UpdateMetaRelationshipCommand(
    Guid Id,
    string RelationshipName,
    string? DeleteRule,
    string? UpdateRule) : IRequest<Result>;

internal sealed class UpdateMetaRelationshipHandler(AppDbContext db)
    : IRequestHandler<UpdateMetaRelationshipCommand, Result>
{
    public async Task<Result> Handle(UpdateMetaRelationshipCommand command, CancellationToken ct)
    {
        var entity = await db.MetaRelationships.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(MetaRelationshipErrors.NotFound(command.Id));
        }

        entity.Update(command.RelationshipName, command.DeleteRule, command.UpdateRule);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
