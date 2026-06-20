using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal record GetAllMetaAttributesQuery(int Offset, int Limit, Guid? MetaTableId)
    : IRequest<Result<PageResponse<MetaAttributeResponse>>>;

internal sealed class GetAllMetaAttributesHandler(AppDbContext db)
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
