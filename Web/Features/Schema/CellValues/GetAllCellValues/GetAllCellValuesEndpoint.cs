using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.CellValues;

internal sealed class GetAllCellValuesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.CellValues.Collection, Handle)
            .WithName("GetAllCellValues")
            .WithTags("Schema")
            .WithSummary("Получить список значений ячеек")
            .WithDescription(
                "Постраничный список значений ячеек EAV. " +
                "422 — Offset < 0 или Limit вне диапазона 1–100.")
            .Produces<PageResponse<CellValueResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllCellValuesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllCellValuesRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllCellValuesQuery, Result<PageResponse<CellValueResponse>>>(
            new GetAllCellValuesQuery(request.Offset, request.Limit), ct);
        return result.ToOk();
    }
}
