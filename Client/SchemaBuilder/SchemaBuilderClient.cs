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
}
