using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal record GetAllSqlQueriesQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<SqlQueryResponse>>>;

internal sealed class GetAllSqlQueriesHandler(AppDbContext db)
    : IRequestHandler<GetAllSqlQueriesQuery, Result<PageResponse<SqlQueryResponse>>>
{
    public async Task<Result<PageResponse<SqlQueryResponse>>> Handle(
        GetAllSqlQueriesQuery query, CancellationToken ct)
    {
        var total = await db.SqlQueries.CountAsync(ct);

        var entities = await db.SqlQueries
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<SqlQueryResponse>>.Success(new PageResponse<SqlQueryResponse>
        {
            Items = entities.Select(SqlQueryMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
