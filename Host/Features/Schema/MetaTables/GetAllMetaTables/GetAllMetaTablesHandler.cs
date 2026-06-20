using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaTables;

internal record GetAllMetaTablesQuery(int Offset, int Limit, Guid? TargetDbId)
    : IRequest<Result<PageResponse<MetaTableResponse>>>;

internal sealed class GetAllMetaTablesHandler(AppDbContext db)
    : IRequestHandler<GetAllMetaTablesQuery, Result<PageResponse<MetaTableResponse>>>
{
    public async Task<Result<PageResponse<MetaTableResponse>>> Handle(
        GetAllMetaTablesQuery query, CancellationToken ct)
    {
        var q = db.MetaTables.AsNoTracking();

        if (query.TargetDbId.HasValue)
        {
            q = q.Where(x => x.TargetDbId == query.TargetDbId.Value);
        }

        var total = await q.CountAsync(ct);

        var entities = await q
            .OrderBy(x => x.TableName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<MetaTableResponse>>.Success(new PageResponse<MetaTableResponse>
        {
            Items = entities.Select(MetaTableMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
