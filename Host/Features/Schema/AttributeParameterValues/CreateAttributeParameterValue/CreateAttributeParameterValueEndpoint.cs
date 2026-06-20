using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal sealed class CreateAttributeParameterValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.AttributeParameterValues.Collection, Handle)
            .WithName("CreateAttributeParameterValue")
            .WithTags("Schema")
            .WithSummary("Задать значение параметра атрибута")
            .WithDescription(
                "Создаёт значение параметра физического типа для указанной колонки. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустые Id, пустое или слишком длинное значение). " +
                "409 — колонка или определение параметра не найдено, либо значение этого " +
                "параметра для колонки уже задано.")
            .Produces<AttributeParameterValueResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateAttributeParameterValueRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateAttributeParameterValueRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<
            CreateAttributeParameterValueCommand,
            Result<AttributeParameterValueResponse>>(
            AttributeParameterValueMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.AttributeParameterValues.ForId(r.Id));
    }
}
