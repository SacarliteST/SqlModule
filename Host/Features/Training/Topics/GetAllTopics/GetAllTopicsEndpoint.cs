using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Training.Topics;

public sealed class GetAllTopicsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Topics.Collection, Handle)
            .WithName("GetAllTopics")
            .WithTags("Training")
            .WithSummary("Список тем с пагинацией")
            .WithDescription(
                "Возвращает 200 OK со страницей тем, отсортированных по названию. " +
                "offset — количество пропускаемых записей (>= 0, по умолчанию 0). " +
                "limit — размер страницы (1–100, по умолчанию 20). " +
                "400 — невалидные параметры пагинации.")
            .Produces<PageResponse<TopicResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllTopicsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllTopicsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllTopicsQuery, Result<PageResponse<TopicResponse>>>(
            new GetAllTopicsQuery(request.Offset, request.Limit), ct);
        return result.ToOk();
    }
}
