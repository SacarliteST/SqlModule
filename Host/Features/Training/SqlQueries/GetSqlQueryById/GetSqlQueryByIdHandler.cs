using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal record GetSqlQueryByIdQuery(Guid Id) : IRequest<Result<SqlQueryResponse>>;

internal sealed class GetSqlQueryByIdHandler(AppDbContext db)
    : IRequestHandler<GetSqlQueryByIdQuery, Result<SqlQueryResponse>>
{
    public async Task<Result<SqlQueryResponse>> Handle(GetSqlQueryByIdQuery query, CancellationToken ct)
    {
        var entity = await db.SqlQueries.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<SqlQueryResponse>.Fail(SqlQueryErrors.NotFound(query.Id));
        }

        return SqlQueryMappings.ToResponse(entity);
    }
}
