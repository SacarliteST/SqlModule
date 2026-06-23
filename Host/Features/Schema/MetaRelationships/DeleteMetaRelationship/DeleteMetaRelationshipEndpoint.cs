using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class DeleteMetaRelationshipEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .WithName("DeleteMetaRelationship")
            .WithTags("Schema", "DevTools")
            .WithSummary("Удалить связь")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Удаляет FK-связь между мета-атрибутами. Сущность листовая — каскадных " +
                "ограничений нет. Возвращает 204 No Content. 404 — связь не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteMetaRelationshipCommand, Result>(
            new DeleteMetaRelationshipCommand(id), ct);
        return result.ToNoContent();
    }
}
