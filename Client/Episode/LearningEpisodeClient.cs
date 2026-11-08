using System.Net;
using System.Net.Http.Json;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Episode;

namespace SQLModule.Client.Episode;

internal sealed class LearningEpisodeClient(HttpClient httpClient) : ILearningEpisodeClient
{
    public async Task<LearningEpisodeResponse> StartAsync(Guid taskId, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync(
            ApiRoutes.Training.Student.ForTaskEpisodes(taskId), null, ct);
        return await ReadAsync(response, ct);
    }

    public async Task<LearningEpisodeResponse> HeartbeatAsync(
        Guid episodeId,
        CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsync(
            ApiRoutes.Training.Student.ForEpisodeHeartbeat(episodeId), null, ct);
        return await ReadAsync(response, ct);
    }

    public async Task<LearningEpisodeResponse> FinalizeAsync(
        Guid episodeId,
        LearningEpisodeFinalizationMode mode,
        Guid? idempotencyKey = null,
        CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            ApiRoutes.Training.Student.ForEpisodeFinalize(episodeId))
        {
            Content = JsonContent.Create(
                new FinalizeLearningEpisodeRequest(mode),
                options: ClientJson.Options)
        };
        request.Headers.Add("Idempotency-Key", (idempotencyKey ?? Guid.NewGuid()).ToString("D"));
        using var response = await httpClient.SendAsync(request, ct);
        return await ReadAsync(response, ct);
    }

    private static async Task<LearningEpisodeResponse> ReadAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException((int)response.StatusCode, null);
        }

        return await response.Content.ReadFromJsonAsync<LearningEpisodeResponse>(ClientJson.Options, ct)
               ?? throw new InvalidResponseFormatException();
    }
}
