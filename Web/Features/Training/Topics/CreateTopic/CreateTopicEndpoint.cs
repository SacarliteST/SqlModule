using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

public sealed class CreateTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Topics.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
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
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
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
