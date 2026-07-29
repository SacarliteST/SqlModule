using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

public sealed class UpdateTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.Topics.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateTopic")
            .WithTags("Training")
            .WithSummary("Обновить тему")
            .WithDescription(
                "Обновляет название существующей темы. " +
                "Возвращает 204 No Content. " +
                "400 — не прошла валидация. " +
                "404 — тема с указанным id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateTopicRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateTopicRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateTopicCommand, Result>(
            TopicMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
