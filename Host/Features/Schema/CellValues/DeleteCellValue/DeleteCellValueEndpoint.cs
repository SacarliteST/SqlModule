using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal sealed class DeleteCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.CellValues.ById, Handle)
            .WithName("DeleteCellValue")
            .WithTags("Schema")
            .WithSummary("Удалить значение ячейки")
            .WithDescription("Удаляет значение ячейки EAV. 404 — значение с указанным Id не найдено.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteCellValueCommand, Result>(
            new DeleteCellValueCommand(id), ct);
        return result.ToNoContent();
    }
}
