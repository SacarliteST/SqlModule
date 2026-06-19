using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal record GetAllTargetDbsQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<TargetDbResponse>>>;

internal sealed class GetAllTargetDbsHandler(AppDbContext db)
    : IRequestHandler<GetAllTargetDbsQuery, Result<PageResponse<TargetDbResponse>>>
{
    public async Task<Result<PageResponse<TargetDbResponse>>> Handle(
        GetAllTargetDbsQuery query, CancellationToken ct)
    {
        var total = await db.TargetDbs.CountAsync(ct);

        var entities = await db.TargetDbs
            .AsNoTracking()
            .OrderBy(x => x.DbName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        var items = entities.Select(TargetDbMappings.ToResponse).ToList();
        return Result<PageResponse<TargetDbResponse>>.Success(new PageResponse<TargetDbResponse>
        {
            Items = items,
            Count = total
        });
    }
}
