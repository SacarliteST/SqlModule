using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class UpdateParameterDefinitionEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.ParameterDefinitions.ById, Handle)
            .WithName("UpdateParameterDefinition")
            .WithTags("DbmsCatalog")
            .WithSummary("Обновить определение параметра")
            .WithDescription(
                "Обновляет поля определения параметра физического типа данных. " +
                "FK (PhysicalTypeId) не изменяется. " +
                "Возвращает 204 No Content. " +
                "422 — не прошла валидация. " +
                "404 — определение параметра с указанным Id не найдено.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateParameterDefinitionRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateParameterDefinitionRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateParameterDefinitionCommand, Result>(
            ParameterDefinitionMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
