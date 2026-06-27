using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class GetParameterDefinitionByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.ParameterDefinitions.ById, Handle)
            .WithName("GetParameterDefinitionById")
            .WithTags("DbmsCatalog")
            .WithSummary("Получить определение параметра по Id")
            .WithDescription(
                "Возвращает определение параметра физического типа данных по идентификатору. " +
                "404 — определение параметра с указанным Id не найдено.")
            .Produces<ParameterDefinitionResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetParameterDefinitionByIdQuery, Result<ParameterDefinitionResponse>>(
            new GetParameterDefinitionByIdQuery(id), ct);
        return result.ToOk();
    }
}
