using System.Net;
using System.Net.Http.Json;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;

namespace SQLModule.Client.Topic;

internal sealed class TopicClient(HttpClient httpClient)
    : CrudClientBase<CreateTopicRequest, UpdateTopicRequest, TopicResponse>(httpClient), ITopicClient
{
    protected override string Collection => ApiRoutes.Training.Topics.Collection;
    protected override string ForId(Guid id) => ApiRoutes.Training.Topics.ForId(id);
    protected override string ForPagination(int offset, int limit)
        => ApiRoutes.Training.Topics.ForPagination(offset, limit);

    public async Task MoveAsync(Guid id, MoveTopicRequest request, CancellationToken ct = default)
    {
        var response = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.Topics.ForParent(id), request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException(
                (int)response.StatusCode,
                await TryReadProblemAsync(response, ct));
        }
    }
}
