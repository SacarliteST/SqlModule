using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.SqlQueries;

internal static class SqlQueryMappings
{
    internal static SqlQueryResponse ToResponse(Domain.Training.SqlQuery e)
    {
        var golden = e.ExpectedResult is null ? null : GoldenResult.Deserialize(e.ExpectedResult);
        return new(
            e.Id,
            e.TargetDbId,
            e.QueryText,
            e.StrictColumnOrder,
            e.StrictRowOrder,
            golden?.Columns ?? [],
            golden?.Rows ?? [],
            e.CreatedById,
            e.CreatedAt,
            e.UpdatedById,
            e.UpdatedAt);
    }

    internal static CreateSqlQueryCommand ToCommand(CreateSqlQueryRequest req) =>
        new(req.TargetDbId, req.QueryText, req.StrictColumnOrder, req.StrictRowOrder);

    internal static UpdateSqlQueryCommand ToCommand(Guid id, UpdateSqlQueryRequest req) =>
        new(id, req.QueryText, req.StrictColumnOrder, req.StrictRowOrder);
}
