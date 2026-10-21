using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaRelationships;

public record UpdateMetaRelationshipCommand(
    Guid Id,
    string RelationshipName,
    string? DeleteRule,
    string? UpdateRule) : IRequest<Result>;

public sealed class UpdateMetaRelationshipHandler(IApplicationDbContext db)
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
