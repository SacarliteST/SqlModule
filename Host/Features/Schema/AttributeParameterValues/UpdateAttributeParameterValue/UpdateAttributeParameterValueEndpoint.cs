using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal sealed class UpdateAttributeParameterValueEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .WithName("UpdateAttributeParameterValue")
            .WithTags("Schema")
            .WithSummary("Обновить значение параметра атрибута")
            .WithDescription(
                "Обновляет строковое значение параметра. " +
                "FK-поля (MetaAttributeId, ParameterDefinitionId) не изменяются. " +
                "Возвращает 204 No Content. " +
                "422 — не прошла валидация (пустое или слишком длинное значение). " +
                "404 — запись с указанным Id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateAttributeParameterValueRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateAttributeParameterValueRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateAttributeParameterValueCommand, Result>(
            AttributeParameterValueMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
