using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal sealed class GetAllAttributeParameterValuesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.AttributeParameterValues.Collection, Handle)
            .WithName("GetAllAttributeParameterValues")
            .WithTags("Schema")
            .WithSummary("Получить список значений параметров атрибутов")
            .WithDescription(
                "Возвращает постраничный список значений параметров физических типов колонок. " +
                "Параметры: offset (≥ 0), limit (1–100), metaAttributeId (опц. — вернуть только " +
                "значения параметров указанной колонки). Результат упорядочен по CreatedAt. " +
                "422 — не прошла валидация пагинации.")
            .Produces<PageResponse<AttributeParameterValueResponse>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllAttributeParameterValuesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllAttributeParameterValuesRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<
            GetAllAttributeParameterValuesQuery,
            Result<PageResponse<AttributeParameterValueResponse>>>(
            new GetAllAttributeParameterValuesQuery(request.Offset, request.Limit, request.MetaAttributeId),
            ct);
        return result.ToOk();
    }
}
