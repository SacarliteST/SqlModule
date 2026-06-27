using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

internal sealed class DeletePhysicalTypeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.DbmsCatalog.PhysicalTypes.ById, Handle)
            .WithName("DeletePhysicalType")
            .WithTags("DbmsCatalog")
            .WithSummary("Удалить физический тип данных")
            .WithDescription(
                "Удаляет физический тип данных. " +
                "Возвращает 204 No Content. " +
                "404 — физический тип с указанным Id не найден. " +
                "409 — физический тип используется мета-атрибутами и не может быть удалён.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeletePhysicalTypeCommand, Result>(
            new DeletePhysicalTypeCommand(id), ct);
        return result.ToNoContent();
    }
}
