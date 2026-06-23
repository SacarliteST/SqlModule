using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.CellValues;

internal sealed class GetCellValueByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.CellValues.ById, Handle)
            .WithName("GetCellValueById")
            .WithTags("Schema")
            .WithSummary("Получить значение ячейки по Id")
            .WithDescription("Возвращает значение ячейки EAV. 404 — значение с указанным Id не найдено.")
            .Produces<CellValueResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetCellValueByIdQuery, Result<CellValueResponse>>(
            new GetCellValueByIdQuery(id), ct);
        return result.ToOk();
    }
}
