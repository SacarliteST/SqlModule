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

    public async Task<ValidateSqlQueryResponse> ValidateAsync(
        ValidateSqlQueryRequest request,
        CancellationToken ct = default)
    {
        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(
            HttpClient,
            ApiRoutes.Training.SqlQueries.Validate,
            request,
            ct);
        return await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<ValidateSqlQueryResponse>(
                   response.Content,
                   ClientJson.Options,
                   ct)
               ?? throw new InvalidResponseFormatException();
    }
}
