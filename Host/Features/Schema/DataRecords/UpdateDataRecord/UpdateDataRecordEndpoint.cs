using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.DataRecord;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal sealed class UpdateDataRecordEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.DataRecords.ById, Handle)
            .WithName("UpdateDataRecord")
            .WithTags("Schema")
            .WithSummary("Обновить строку данных")
            .WithDescription(
                "Обновляет порядок отображения строки данных (SortOrder). " +
                "FK-поля (MetaTableId) не изменяются. " +
                "Возвращает 204 No Content. " +
                "422 — не прошла валидация (отрицательный SortOrder). " +
                "404 — строка с указанным Id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateDataRecordRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateDataRecordRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateDataRecordCommand, Result>(
            DataRecordMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
