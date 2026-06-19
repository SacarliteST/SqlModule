using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class DeleteTargetDbEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.TargetDbs.ById, Handle)
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
