using System.Net.Http.Json;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Client.SchemaBuilder;

internal sealed class SchemaBuilderClient(HttpClient httpClient) : ISchemaBuilderClient
{
    public async Task ValidateAsync(CreateSchemaRequest request, CancellationToken ct = default)
        => await httpClient.PostAsJsonAsync(ApiRoutes.Schema.SchemaBuilder.Validate, request, ClientJson.Options, ct);

    public async Task<CreateSchemaResponse> CreateAsync(CreateSchemaRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.Schema.SchemaBuilder.Collection, request, ClientJson.Options, ct);
        return await response.Content.ReadFromJsonAsync<CreateSchemaResponse>(ClientJson.Options, ct)
               ?? throw new InvalidResponseFormatException();
    }

    public async Task<TargetDbSchemaResponse?> GetTargetDbSchemaAsync(Guid targetDbId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(ApiRoutes.Schema.TargetDbs.ForSchema(targetDbId), ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<TargetDbSchemaResponse>(response, ct);
    }

    public async Task<SchemaValidationResponse> ValidateTargetDbSchemaAsync(
        Guid targetDbId, SchemaUpsertRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.Schema.TargetDbs.ForValidateSchema(targetDbId), request, ClientJson.Options, ct);
        return await ReadAsync<SchemaValidationResponse>(response, ct);
    }

    public async Task<TargetDbSchemaResponse> ApplyTargetDbSchemaAsync(
        Guid targetDbId, SchemaUpsertRequest request, string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, ApiRoutes.Schema.TargetDbs.ForSchema(targetDbId))
        {
            Content = JsonContent.Create(request, options: ClientJson.Options)
        };
        message.Headers.TryAddWithoutValidation("If-Match", $"\"{request.Version}\"");
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        return await ReadAsync<TargetDbSchemaResponse>(await httpClient.SendAsync(message, ct), ct);
    }

    public async Task<TableRowsResponse?> GetTableRowsAsync(
        Guid targetDbId, Guid tableId, int offset, int limit, CancellationToken ct = default)
    {
        var path = $"{ApiRoutes.Schema.TargetDbs.ForTableRows(targetDbId, tableId)}?offset={offset}&limit={limit}";
        var response = await httpClient.GetAsync(path, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadAsync<TableRowsResponse>(response, ct);
    }

    public async Task<LookupValuesResponse> GetLookupValuesAsync(
        Guid targetDbId,
        Guid tableId,
        Guid valueColumnId,
        Guid? labelColumnId = null,
        string? search = null,
        int offset = 0,
        int limit = 30,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"valueColumnId={valueColumnId:D}",
            $"offset={offset}",
            $"limit={limit}"
        };
        if (labelColumnId.HasValue)
        {
            query.Add($"labelColumnId={labelColumnId.Value:D}");
        }

        if (!String.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        var path = $"{ApiRoutes.Schema.TargetDbs.ForLookupValues(targetDbId, tableId)}?{String.Join('&', query)}";
        return await ReadAsync<LookupValuesResponse>(await httpClient.GetAsync(path, ct), ct);
    }

    public async Task<BatchTableRowsResponse> SaveTableRowsAsync(
        Guid targetDbId, Guid tableId, string idempotencyKey,
        BatchTableRowsRequest request, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Patch, ApiRoutes.Schema.TargetDbs.ForTableRows(targetDbId, tableId))
        {
            Content = JsonContent.Create(request, options: ClientJson.Options)
        };
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return await ReadAsync<BatchTableRowsResponse>(await httpClient.SendAsync(message, ct), ct);
    }

    public async Task<ValidateTargetDbDdlResponse> ValidateTargetDbDdlAsync(
        ValidateTargetDbDdlRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            ApiRoutes.Schema.TargetDbs.ValidateDdl, request, ClientJson.Options, ct);
        return await ReadAsync<ValidateTargetDbDdlResponse>(response, ct);
    }

    public async Task<CreateTargetDbFromDdlResponse> CreateTargetDbFromDdlAsync(
        string idempotencyKey, CreateTargetDbFromDdlRequest request, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Schema.TargetDbs.FromDdl)
        {
            Content = JsonContent.Create(request, options: ClientJson.Options)
        };
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        return await ReadAsync<CreateTargetDbFromDdlResponse>(await httpClient.SendAsync(message, ct), ct);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<T>(ClientJson.Options, ct)
        ?? throw new InvalidResponseFormatException();
}
