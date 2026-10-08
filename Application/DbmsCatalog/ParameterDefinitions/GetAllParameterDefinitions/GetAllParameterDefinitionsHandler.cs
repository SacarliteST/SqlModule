using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public record GetAllParameterDefinitionsQuery(int Offset, int Limit, Guid? PhysicalTypeId)
    : IRequest<Result<PageResponse<ParameterDefinitionResponse>>>;

public sealed class GetAllParameterDefinitionsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllParameterDefinitionsQuery, Result<PageResponse<ParameterDefinitionResponse>>>
{
    public async Task<Result<PageResponse<ParameterDefinitionResponse>>> Handle(
        GetAllParameterDefinitionsQuery query, CancellationToken ct)
    {
        var source = db.ParameterDefinitions.AsNoTracking();

        if (query.PhysicalTypeId.HasValue)
        {
            source = source.Where(p => p.PhysicalTypeId == query.PhysicalTypeId.Value);
        }

        var total = await source.CountAsync(ct);
        var entities = await source
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.ParameterKey)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<ParameterDefinitionResponse>>.Success(new PageResponse<ParameterDefinitionResponse>
        {
            Items = entities.Select(ParameterDefinitionMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
