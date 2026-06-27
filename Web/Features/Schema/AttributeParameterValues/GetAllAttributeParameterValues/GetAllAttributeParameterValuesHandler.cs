using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.AttributeParameterValues;

internal record GetAllAttributeParameterValuesQuery(int Offset, int Limit, Guid? MetaAttributeId)
    : IRequest<Result<PageResponse<AttributeParameterValueResponse>>>;

internal sealed class GetAllAttributeParameterValuesHandler(AppDbContext db)
    : IRequestHandler<GetAllAttributeParameterValuesQuery, Result<PageResponse<AttributeParameterValueResponse>>>
{
    public async Task<Result<PageResponse<AttributeParameterValueResponse>>> Handle(
        GetAllAttributeParameterValuesQuery query, CancellationToken ct)
    {
        var source = db.AttributeParameterValues.AsNoTracking();

        if (query.MetaAttributeId.HasValue)
        {
            source = source.Where(v => v.MetaAttributeId == query.MetaAttributeId.Value);
        }

        var total = await source.CountAsync(ct);
        var entities = await source
            .OrderBy(v => v.CreatedAt)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<AttributeParameterValueResponse>>.Success(
            new PageResponse<AttributeParameterValueResponse>
            {
                Items = entities.Select(AttributeParameterValueMappings.ToResponse).ToList(),
                Count = total
            });
    }
}
