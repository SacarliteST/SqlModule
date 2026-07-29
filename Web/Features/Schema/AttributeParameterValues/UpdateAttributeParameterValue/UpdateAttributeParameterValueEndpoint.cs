using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.AttributeParameterValues;

internal sealed class UpdateAttributeParameterValueEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.AttributeParameterValues.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateAttributeParameterValue")
            .WithTags("Schema", "DevTools")
            .WithSummary("Обновить значение параметра атрибута")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Обновляет строковое значение параметра. " +
                "FK-поля (MetaAttributeId, ParameterDefinitionId) не изменяются. " +
                "Возвращает 204 No Content. " +
                "422 — не прошла валидация (пустое или слишком длинное значение). " +
                "404 — запись с указанным Id не найдена.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
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
