using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlQueries;

public sealed class DeleteSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Training.SqlQueries.ById, Handle)
            .WithName("DeleteSqlQuery")
            .WithTags("Training")
            .WithSummary("Удалить SQL-запрос")
            .WithDescription(
                "Удаляет SQL-запрос по Id. " +
                "Возвращает 204 No Content. " +
                "404 — запрос не найден. " +
                "409 — запрос используется заданиями или попытками.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteSqlQueryCommand, Result>(
            new DeleteSqlQueryCommand(id), ct);
        return result.ToNoContent();
    }
}
