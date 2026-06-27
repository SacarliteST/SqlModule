using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class DeleteParameterDefinitionEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.DbmsCatalog.ParameterDefinitions.ById, Handle)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteParameterDefinition")
            .WithTags("DbmsCatalog")
            .WithSummary("Удалить определение параметра")
            .WithDescription(
                "Удаляет определение параметра физического типа данных. " +
                "Удаление каскадно сносит все значения параметра у колонок (AttributeParameterValue). " +
                "Возвращает 204 No Content. " +
                "404 — определение параметра с указанным Id не найдено.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteParameterDefinitionCommand, Result>(
            new DeleteParameterDefinitionCommand(id), ct);
        return result.ToNoContent();
    }
}
