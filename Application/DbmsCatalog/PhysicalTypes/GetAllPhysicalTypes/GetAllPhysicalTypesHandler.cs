using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.PhysicalTypes;

public record GetAllPhysicalTypesQuery(int Offset, int Limit, Guid? DbmsId)
    : IRequest<Result<PageResponse<PhysicalTypeResponse>>>;

public sealed class GetAllPhysicalTypesHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllPhysicalTypesQuery, Result<PageResponse<PhysicalTypeResponse>>>
{
    public async Task<Result<PageResponse<PhysicalTypeResponse>>> Handle(
        GetAllPhysicalTypesQuery query, CancellationToken ct)
    {
        var source = db.PhysicalTypes.AsNoTracking();

        if (query.DbmsId.HasValue)
        {
            source = source.Where(p => p.DbmsId == query.DbmsId.Value);
        }

        var total = await source.CountAsync(ct);
        var entities = await source
            .OrderBy(p => p.TypeName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<PhysicalTypeResponse>>.Success(new PageResponse<PhysicalTypeResponse>
        {
            Items = entities.Select(PhysicalTypeMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
