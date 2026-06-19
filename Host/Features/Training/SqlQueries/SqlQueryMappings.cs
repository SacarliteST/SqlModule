using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Host.Features.Training.SqlQueries;

internal static class SqlQueryMappings
{
    internal static SqlQueryResponse ToResponse(Domain.Training.SqlQuery e) => new(
        e.Id, e.QueryText, e.StrictColumnOrder, e.StrictRowOrder,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateSqlQueryCommand ToCommand(CreateSqlQueryRequest req) =>
        new(req.QueryText, req.StrictColumnOrder, req.StrictRowOrder);

    internal static UpdateSqlQueryCommand ToCommand(Guid id, UpdateSqlQueryRequest req) =>
        new(id, req.QueryText, req.StrictColumnOrder, req.StrictRowOrder);
}
