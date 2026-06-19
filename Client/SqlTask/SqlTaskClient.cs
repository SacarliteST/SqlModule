using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Client.SqlTask;

internal sealed class SqlTaskClient(HttpClient httpClient)
    : CrudClientBase<CreateSqlTaskRequest, UpdateSqlTaskRequest, SqlTaskResponse>(httpClient), ISqlTaskClient
{
    protected override string Collection => ApiRoutes.Training.SqlTasks.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Training.SqlTasks.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Training.SqlTasks.ForPagination(offset, limit);
}
