using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

public sealed class DeleteTopicEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Training.Topics.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("DeleteTopic")
            .WithTags("Training")
            .WithSummary("Удалить тему")
            .WithDescription(
                "Удаляет тему по Id. " +
                "Возвращает 204 No Content. " +
                "404 — тема не найдена. " +
                "409 — тема содержит подтемы или задания.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteTopicCommand, Result>(
            new DeleteTopicCommand(id), ct);
        return result.ToNoContent();
    }
}
