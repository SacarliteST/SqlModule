using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal record GetAllMetaRelationshipsQuery(int Offset, int Limit, Guid? AttributeId)
    : IRequest<Result<PageResponse<MetaRelationshipResponse>>>;

internal sealed class GetAllMetaRelationshipsHandler(AppDbContext db)
    : IRequestHandler<GetAllMetaRelationshipsQuery, Result<PageResponse<MetaRelationshipResponse>>>
{
    public async Task<Result<PageResponse<MetaRelationshipResponse>>> Handle(
        GetAllMetaRelationshipsQuery query, CancellationToken ct)
    {
        var source = db.MetaRelationships.AsNoTracking();

        if (query.AttributeId.HasValue)
        {
            source = source.Where(r =>
                r.SourceAttributeId == query.AttributeId.Value ||
                r.TargetAttributeId == query.AttributeId.Value);
        }

        var total = await source.CountAsync(ct);
        var entities = await source
            .OrderBy(r => r.RelationshipName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<MetaRelationshipResponse>>.Success(new PageResponse<MetaRelationshipResponse>
        {
            Items = entities.Select(MetaRelationshipMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
