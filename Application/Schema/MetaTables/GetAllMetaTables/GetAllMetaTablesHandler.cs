using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Schema.MetaTables;

public record GetAllMetaTablesQuery(int Offset, int Limit, Guid? TargetDbId)
    : IRequest<Result<PageResponse<MetaTableResponse>>>;

public sealed class GetAllMetaTablesHandler(IApplicationDbContext db)
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
