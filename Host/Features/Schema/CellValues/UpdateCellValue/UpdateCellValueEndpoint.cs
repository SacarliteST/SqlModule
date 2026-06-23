using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal sealed class UpdateCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.CellValues.ById, Handle)
            .WithName("UpdateCellValue")
            .WithTags("Schema")
            .WithSummary("Обновить значение ячейки")
            .WithDescription(
                "Обновляет текстовое значение ячейки EAV. " +
                "422 — TextValue > 2000 символов. " +
                "404 — значение с указанным Id не найдено.")
            .Produces<CellValueResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateCellValueRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateCellValueRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateCellValueCommand, Result<CellValueResponse>>(
            new UpdateCellValueCommand(id, request.TextValue), ct);
        return result.ToOk();
    }
}
