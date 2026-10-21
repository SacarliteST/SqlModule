using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.SqlQueries;

public record GetAllSqlQueriesQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<SqlQueryResponse>>>;

public sealed class GetAllSqlQueriesHandler(IApplicationDbContext db)
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
