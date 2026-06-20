using System.Net.Http.Json;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;

namespace SQLModule.Client.DbmsDictionary;

internal sealed class DbmsDictionaryClient(HttpClient httpClient)
    : CrudClientBase<CreateDbmsDictionaryRequest, UpdateDbmsDictionaryRequest, DbmsDictionaryResponse>(httpClient),
        IDbmsDictionaryClient
{
    protected override string Collection => ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection;
    protected override string ForId(Guid id) => ApiRoutes.DbmsCatalog.DbmsDictionaries.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.DbmsCatalog.DbmsDictionaries.ForPagination(offset, limit);

    public async Task ValidateAsync(CreateDbmsDictionaryRequest request, CancellationToken ct = default)
    {
        await HttpClient.PostAsJsonAsync(
            ApiRoutes.DbmsCatalog.DbmsDictionaries.Validate, request, ClientJson.Options, ct);
    }
}
