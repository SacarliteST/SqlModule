using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.CellValues;

internal sealed class DeleteCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.CellValues.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
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
