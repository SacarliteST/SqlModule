using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

public sealed class CreateTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Topics.Collection, Handle)
            .WithName("CreateTopic")
            .WithTags("Training")
            .WithSummary("Создать тему")
            .WithDescription(
                "Создаёт новую тему тренажёра. " +
                "Возвращает 201 Created с телом ответа. " +
                "ParentTopicId = null — корневая тема. " +
                "400 — не прошла валидация. " +
                "409 — родительская тема с указанным ParentTopicId не найдена.")
            .Produces<TopicResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateTopicRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateTopicRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateTopicCommand, Result<TopicResponse>>(
            TopicMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Training.Topics.ForId(r.Id));
    }
}
