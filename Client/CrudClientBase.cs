using System.Net;
using System.Net.Http.Json;
using SQLModule.Contracts;

namespace SQLModule.Client;

/// <summary>
/// Базовая реализация <see cref="ICrudClient{TCreate,TUpdate,TResponse}"/>.
/// После <see cref="ErrorDelegatingHandler"/> метод получает только 2xx- или 404-ответ.
/// </summary>
internal abstract class CrudClientBase<TCreateRequest, TUpdateRequest, TResponse>
    : ICrudClient<TCreateRequest, TUpdateRequest, TResponse>
    where TCreateRequest : class
    where TUpdateRequest : class
    where TResponse : class
{
    protected readonly HttpClient HttpClient;

    protected CrudClientBase(HttpClient httpClient) => this.HttpClient = httpClient;

    /// <summary>Относительный URL коллекции (без ведущего '/').</summary>
    protected abstract string Collection { get; }

    /// <summary>Относительный URL элемента по Id.</summary>
    protected abstract string ForId(Guid id);

    /// <summary>Относительный URL для пагинации.</summary>
    protected abstract string ForPagination(int offset, int limit);

    /// <inheritdoc/>
    public async Task<TResponse> CreateAsync(TCreateRequest request, CancellationToken ct = default)
    {
        var response = await HttpClient.PostAsJsonAsync(Collection, request, ct);
        return await ReadRequiredAsync<TResponse>(response, ct);
    }

    /// <inheritdoc/>
    public async Task<TResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await HttpClient.GetAsync(ForId(id), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadRequiredAsync<TResponse>(response, ct);
    }

    /// <inheritdoc/>
    public async Task<PageResponse<TResponse>> GetAllAsync(int offset, int limit, CancellationToken ct = default)
    {
        var response = await HttpClient.GetAsync(ForPagination(offset, limit), ct);
        return await ReadRequiredAsync<PageResponse<TResponse>>(response, ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Guid id, TUpdateRequest request, CancellationToken ct = default)
    {
        var response = await HttpClient.PutAsJsonAsync(ForId(id), request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException((int)response.StatusCode, await TryReadProblemAsync(response, ct));
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // 404 → идемпотентный no-op; 204 → успех; ошибки уже брошены ErrorDelegatingHandler
        await HttpClient.DeleteAsync(ForId(id), ct);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<T>(ClientJson.Options, ct)
               ?? throw new InvalidResponseFormatException();
    }

    protected static async Task<ApiProblem?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiProblem>(ClientJson.Options, ct);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
