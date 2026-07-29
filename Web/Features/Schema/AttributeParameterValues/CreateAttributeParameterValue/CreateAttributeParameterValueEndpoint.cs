using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.AttributeParameterValues;

internal sealed class CreateAttributeParameterValueEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.AttributeParameterValues.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateAttributeParameterValue")
            .WithTags("Schema", "DevTools")
            .WithSummary("Задать значение параметра атрибута")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Создаёт значение параметра физического типа для указанной колонки. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустые Id, пустое или слишком длинное значение). " +
                "409 — колонка или определение параметра не найдено, либо значение этого " +
                "параметра для колонки уже задано.")
            .Produces<AttributeParameterValueResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
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
