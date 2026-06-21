using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal record CreateSqlQueryCommand(string QueryText, bool StrictColumnOrder, bool StrictRowOrder)
    : IRequest<Result<SqlQueryResponse>>;

internal sealed class CreateSqlQueryHandler(AppDbContext db)
    : IRequestHandler<CreateSqlQueryCommand, Result<SqlQueryResponse>>
{
    public async Task<Result<SqlQueryResponse>> Handle(CreateSqlQueryCommand command, CancellationToken ct)
    {
        var entity = Domain.Training.SqlQuery.Create(
            command.QueryText, command.StrictColumnOrder, command.StrictRowOrder);

        db.SqlQueries.Add(entity);
        await db.SaveChangesAsync(ct);
        return SqlQueryMappings.ToResponse(entity);
    }
}
