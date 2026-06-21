using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal sealed class DeleteDataRecordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Schema.DataRecords.ById, Handle)
            .WithName("DeleteDataRecord")
            .WithTags("Schema")
            .WithSummary("Удалить строку данных")
            .WithDescription(
                "Удаляет строку данных (EAV-якорь). " +
                "Удаление каскадно сносит все ячейки строки (CellValue). " +
                "На DataRecord никто более не ссылается — конфликтов не возникает. " +
                "Возвращает 204 No Content. " +
                "404 — строка с указанным Id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteDataRecordCommand, Result>(
            new DeleteDataRecordCommand(id), ct);
        return result.ToNoContent();
    }
}
