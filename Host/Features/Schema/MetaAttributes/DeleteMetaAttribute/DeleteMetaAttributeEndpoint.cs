using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal sealed class DeleteMetaAttributeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.MetaAttributes.ById, Handle)
            .WithName("DeleteMetaAttribute")
            .WithTags("Schema")
            .WithSummary("Удалить мета-атрибут")
            .WithDescription(
                "Удаляет мета-атрибут (колонку) таблицы. " +
                "Удаление каскадно сносит все ячейки (CellValue) и значения параметров " +
                "(AttributeParameterValue) данной колонки. " +
                "Возвращает 204 No Content. " +
                "404 — мета-атрибут с указанным Id не найден. " +
                "409 — колонка участвует в FK-связи (MetaRelationship) и не может быть удалена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteMetaAttributeCommand, Result>(
            new DeleteMetaAttributeCommand(id), ct);
        return result.ToNoContent();
    }
}
