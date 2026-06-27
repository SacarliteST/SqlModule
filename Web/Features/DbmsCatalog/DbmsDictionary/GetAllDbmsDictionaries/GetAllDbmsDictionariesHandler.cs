using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;

internal record GetAllDbmsDictionariesQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<DbmsDictionaryResponse>>>;

internal sealed class GetAllDbmsDictionariesHandler(AppDbContext db)
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
            Items = entities.Select(DbmsDictionaryMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
