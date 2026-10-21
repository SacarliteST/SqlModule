using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public record GetAllAttributeParameterValuesQuery(int Offset, int Limit, Guid? MetaAttributeId)
    : IRequest<Result<PageResponse<AttributeParameterValueResponse>>>;

public sealed class GetAllAttributeParameterValuesHandler(IApplicationDbContext db)
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
