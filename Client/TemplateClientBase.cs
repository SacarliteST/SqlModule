using System.Net;
using System.Net.Http.Json;
using Contracts;

namespace TemplateApi.Client;

internal abstract class TemplateClientBase<TResponse, TDetailedResponse, TRequest, TKey> :
    ITemplateClientBase<TResponse, TDetailedResponse, TRequest, TKey>
    where TResponse : class
    where TDetailedResponse : class
    where TRequest : class
{
    protected readonly HttpClient HttpClient;
    protected readonly Uri BaseUri;

    public TemplateClientBase(HttpClient httpClient, Uri baseUri)
    {
        HttpClient = httpClient;
        BaseUri = baseUri;
    }

    public async Task<TResponse> CreateAsync(TRequest createRequest, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(BaseUrl, createRequest, cancellationToken);
        return await ReadFromResponseOrThrowAsync<TResponse>(response, cancellationToken);
    }

    public async Task DeleteAsync(TKey id, CancellationToken cancellationToken)
    {
        var url = GetUriForId(id);
        var response = await HttpClient.DeleteAsync(
            url,
            cancellationToken);
    }

    public async Task<PageResponse<TResponse>> GetWithPagingAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var url = GetUriForPagination(offset, limit);
        var response = await HttpClient.GetAsync(
            url,
            cancellationToken);

        return await ReadFromResponseOrThrowAsync<PageResponse<TResponse>>(response, cancellationToken);
    }

    public async Task<TDetailedResponse?> GetByIdAsync(TKey id, CancellationToken cancellationToken)
    {
        var url = GetUriForId(id);
        var response = await HttpClient.GetAsync(
            url,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadFromResponseOrThrowAsync<TDetailedResponse>(response, cancellationToken);
    }

    public async Task<TResponse?> UpdateAsync(
        TKey id,
        TRequest updateRequest,
        CancellationToken cancellationToken)
    {
        var url = GetUriForId(id);
        var response = await HttpClient.PutAsJsonAsync(
            url,
            updateRequest,
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadFromResponseOrThrowAsync<TResponse>(response, cancellationToken);
    }

    protected abstract Uri GetUriForId(TKey id);

    protected abstract Uri GetUriForPagination(int offset, int limit);

    protected abstract Uri BaseUrl { get; }

    private static async Task<T> ReadFromResponseOrThrowAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
                ?? throw new InvalidResponseFormatException();
    }
}
