using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal record CreateMetaRelationshipCommand(
    string RelationshipName,
    Guid SourceAttributeId,
    Guid TargetAttributeId,
    string? DeleteRule,
    string? UpdateRule) : IRequest<Result<MetaRelationshipResponse>>;

internal sealed class CreateMetaRelationshipHandler(AppDbContext db)
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
