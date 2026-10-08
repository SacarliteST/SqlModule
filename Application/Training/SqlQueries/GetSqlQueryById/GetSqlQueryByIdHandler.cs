using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Training.SqlQueries;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.SqlQueries;

public record GetSqlQueryByIdQuery(Guid Id) : IRequest<Result<SqlQueryResponse>>;

public sealed class GetSqlQueryByIdHandler(IApplicationDbContext db)
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
