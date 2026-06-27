using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.TargetDbs;

public sealed class DeleteTargetDbEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.TargetDbs.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("DeleteTargetDb")
            .WithTags("Schema")
            .WithSummary("Удалить целевую БД")
            .WithDescription(
                "Удаляет БД-песочницу по Id. " +
                "Возвращает 204 No Content. " +
                "404 — запись с указанным id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteTargetDbCommand, Result>(
            new DeleteTargetDbCommand(id), ct);
        return result.ToNoContent();
    }
}
