using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.TargetDbs;

public record GetAllTargetDbsQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<TargetDbResponse>>>;

public sealed class GetAllTargetDbsHandler(IApplicationDbContext db)
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
