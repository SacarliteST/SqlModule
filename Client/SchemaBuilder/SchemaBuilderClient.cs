using System.Net.Http.Json;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Client.SchemaBuilder;

internal sealed class SchemaBuilderClient(HttpClient httpClient) : ISchemaBuilderClient
{
    public async Task ValidateAsync(CreateSchemaRequest request, CancellationToken ct = default)
        => await httpClient.PostAsJsonAsync(ApiRoutes.Schema.SchemaBuilder.Validate, request, ClientJson.Options, ct);
}
