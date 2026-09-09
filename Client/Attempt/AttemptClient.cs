using System.Net;
using System.Net.Http.Json;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Client.Attempt;

internal sealed class AttemptClient(HttpClient httpClient) : IAttemptClient
{
    public async Task<SubmitAttemptResponse> SubmitAsync(
        SubmitAttemptRequest request, Guid? idempotencyKey = null, CancellationToken ct = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Training.Attempts.Collection)
        {
            Content = JsonContent.Create(request, options: ClientJson.Options)
        };
        message.Headers.Add("Idempotency-Key", (idempotencyKey ?? Guid.NewGuid()).ToString("D"));
        var response = await httpClient.SendAsync(message, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException((int)response.StatusCode, await TryReadProblemAsync(response, ct));
        }

        return await ReadRequiredAsync<SubmitAttemptResponse>(response, ct);
    }

    public async Task<AttemptResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            ApiRoutes.Training.Attempts.ForId(id), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadRequiredAsync<AttemptResponse>(response, ct);
    }

    public async Task<PageResponse<AttemptListItemResponse>> GetAllAsync(
        int offset, int limit, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            ApiRoutes.Training.Attempts.ForPagination(offset, limit), ct);
        return await ReadRequiredAsync<PageResponse<AttemptListItemResponse>>(response, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await httpClient.DeleteAsync(ApiRoutes.Training.Attempts.ForId(id), ct);
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<T>(ClientJson.Options, ct)
               ?? throw new InvalidResponseFormatException();
    }

    private static async Task<ApiProblem?> TryReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
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
