using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.MetaTables;

public sealed class DeleteMetaTableEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.MetaTables.ById, Handle)
            .WithName("DeleteMetaTable")
            .WithTags("Schema", "DevTools")
            .WithSummary("Удалить мета-таблицу")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Удаляет мета-таблицу. " +
                "Каскадно удаляет все колонки (MetaAttribute), строки данных (DataRecord) и ячейки (CellValue). " +
                "Возвращает 204 No Content. " +
                "404 — мета-таблица не найдена. " +
                "409 — одна или несколько колонок таблицы участвуют в MetaRelationship (FK).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteMetaTableCommand, Result>(
            new DeleteMetaTableCommand(id), ct);
        return result.ToNoContent();
    }
}
