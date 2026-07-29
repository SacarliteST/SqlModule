using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.CellValue;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.CellValues;

internal sealed class CreateCellValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.CellValues.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateCellValue")
            .WithTags("Schema")
            .WithSummary("Создать значение ячейки")
            .WithDescription(
                "Создаёт значение ячейки EAV для заданной строки и атрибута. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустые Id, TextValue > 2000 символов). " +
                "409 — DataRecord или MetaAttribute с указанным Id не найдены.")
            .Produces<CellValueResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateCellValueRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateCellValueRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateCellValueCommand, Result<CellValueResponse>>(
            CellValueMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.CellValues.ForId(r.Id));
    }
}
