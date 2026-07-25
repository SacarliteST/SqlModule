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

    public async Task<TeacherTaskDetailsResponse?> GetTeacherDetailsAsync(
        Guid taskId,
        CancellationToken ct = default)
    {
        var response = await HttpClient.GetAsync(ApiRoutes.Training.TeacherTasks.ForDetails(taskId), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<TeacherTaskDetailsResponse>(
                   response.Content,
                   ClientJson.Options,
                   ct)
               ?? throw new InvalidResponseFormatException();
    }
}
