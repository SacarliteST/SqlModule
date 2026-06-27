using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

public sealed class MoveTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.Topics.Parent, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("MoveTopic")
            .WithTags("Training")
            .WithSummary("Переместить тему")
            .WithDescription(
                "Перемещает тему в иерархии. " +
                "Возвращает 204 No Content. " +
                "404 — тема с указанным id не найдена. " +
                "409 — новый родитель не найден или перемещение создаёт цикл.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id, MoveTopicRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<MoveTopicCommand, Result>(
            TopicMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
