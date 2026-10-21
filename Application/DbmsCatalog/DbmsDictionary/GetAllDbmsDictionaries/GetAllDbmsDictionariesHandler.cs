using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;

public record GetAllDbmsDictionariesQuery(int Offset, int Limit, bool CanManageCatalog)
    : IRequest<Result<PageResponse<DbmsDictionaryResponse>>>;

public sealed class GetAllDbmsDictionariesHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllDbmsDictionariesQuery, Result<PageResponse<DbmsDictionaryResponse>>>
{
    public async Task<Result<PageResponse<DbmsDictionaryResponse>>> Handle(
        GetAllDbmsDictionariesQuery query, CancellationToken ct)
    {
        var total = await db.DbmsDictionaries.CountAsync(ct);
        var entities = await db.DbmsDictionaries
            .AsNoTracking()
            .OrderBy(x => x.DbmsName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<DbmsDictionaryResponse>>.Success(new PageResponse<DbmsDictionaryResponse>
        {
            Items = entities.Select(entity => DbmsDictionaryMappings.ToResponse(entity) with
            {
                CanManageCatalog = query.CanManageCatalog
            }).ToList(),
            Count = total
        });
    }
}
