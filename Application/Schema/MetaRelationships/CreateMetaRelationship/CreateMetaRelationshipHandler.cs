using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaRelationships;

public record CreateMetaRelationshipCommand(
    string RelationshipName,
    Guid SourceAttributeId,
    Guid TargetAttributeId,
    string? DeleteRule,
    string? UpdateRule) : IRequest<Result<MetaRelationshipResponse>>;

public sealed class CreateMetaRelationshipHandler(IApplicationDbContext db)
    : IRequestHandler<CreateMetaRelationshipCommand, Result<MetaRelationshipResponse>>
{
    public async Task<Result<MetaRelationshipResponse>> Handle(
        CreateMetaRelationshipCommand command, CancellationToken ct)
    {
        if (!await db.MetaAttributes.AnyAsync(x => x.Id == command.SourceAttributeId, ct))
        {
            return Result<MetaRelationshipResponse>.Fail(
                MetaRelationshipErrors.SourceAttributeNotFound(command.SourceAttributeId));
        }

        if (!await db.MetaAttributes.AnyAsync(x => x.Id == command.TargetAttributeId, ct))
        {
            return Result<MetaRelationshipResponse>.Fail(
                MetaRelationshipErrors.TargetAttributeNotFound(command.TargetAttributeId));
        }

        var entity = MetaRelationship.Create(
            command.RelationshipName, command.SourceAttributeId, command.TargetAttributeId,
            command.DeleteRule, command.UpdateRule);

        db.MetaRelationships.Add(entity);
        await db.SaveChangesAsync(ct);
        return MetaRelationshipMappings.ToResponse(entity);
    }
}
