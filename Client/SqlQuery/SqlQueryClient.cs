using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;

namespace SQLModule.Client.SqlQuery;

internal sealed class SqlQueryClient(HttpClient httpClient)
    : CrudClientBase<CreateSqlQueryRequest, UpdateSqlQueryRequest, SqlQueryResponse>(httpClient), ISqlQueryClient
{
    protected override string Collection => ApiRoutes.Training.SqlQueries.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Training.SqlQueries.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Training.SqlQueries.ForPagination(offset, limit);
}
