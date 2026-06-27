using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

public sealed class DeleteSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Training.SqlTasks.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("DeleteSqlTask")
            .WithTags("Training")
            .WithSummary("Удалить SQL-задание")
            .WithDescription(
                "Удаляет задание по Id. " +
                "Возвращает 204 No Content. " +
                "404 — задание не найдено. " +
                "409 — задание имеет попытки выполнения.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteSqlTaskCommand, Result>(
            new DeleteSqlTaskCommand(id), ct);
        return result.ToNoContent();
    }
}
