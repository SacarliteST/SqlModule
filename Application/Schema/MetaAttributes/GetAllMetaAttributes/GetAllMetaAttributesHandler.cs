using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaAttributes;

public record GetAllMetaAttributesQuery(int Offset, int Limit, Guid? MetaTableId)
    : IRequest<Result<PageResponse<MetaAttributeResponse>>>;

public sealed class GetAllMetaAttributesHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllMetaAttributesQuery, Result<PageResponse<MetaAttributeResponse>>>
{
    public async Task<Result<PageResponse<MetaAttributeResponse>>> Handle(
        GetAllMetaAttributesQuery query, CancellationToken ct)
    {
        var source = db.MetaAttributes.AsNoTracking();

        if (query.MetaTableId.HasValue)
        {
            source = source.Where(a => a.MetaTableId == query.MetaTableId.Value);
        }

        var total = await source.CountAsync(ct);
        var entities = await source
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.AttributeName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<MetaAttributeResponse>>.Success(new PageResponse<MetaAttributeResponse>
        {
            Items = entities.Select(MetaAttributeMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
