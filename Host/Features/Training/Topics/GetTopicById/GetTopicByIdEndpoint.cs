using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

public sealed class GetTopicByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Topics.ById, Handle)
            .WithName("GetTopicById")
            .WithTags("Training")
            .WithSummary("Получить тему по Id")
            .WithDescription(
                "Возвращает 200 OK с данными темы. " +
                "404 — тема с указанным id не найдена.")
            .Produces<TopicResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetTopicByIdQuery, Result<TopicResponse>>(
            new GetTopicByIdQuery(id), ct);
        return result.ToOk();
    }
}
