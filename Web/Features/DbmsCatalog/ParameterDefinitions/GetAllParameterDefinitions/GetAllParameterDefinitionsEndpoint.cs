using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class GetAllParameterDefinitionsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllParameterDefinitions")
            .WithTags("DbmsCatalog")
            .WithSummary("Получить список определений параметров")
            .WithDescription(
                "Возвращает постраничный список определений параметров физических типов данных. " +
                "Параметры: offset (≥ 0), limit (1–100), physicalTypeId (опц. — фильтрация по типу). " +
                "Результат упорядочен по SortOrder, затем по ParameterKey. " +
                "422 — не прошла валидация пагинации.")
            .Produces<PageResponse<ParameterDefinitionResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllParameterDefinitionsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllParameterDefinitionsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllParameterDefinitionsQuery, Result<PageResponse<ParameterDefinitionResponse>>>(
            new GetAllParameterDefinitionsQuery(request.Offset, request.Limit, request.PhysicalTypeId), ct);
        return result.ToOk();
    }
}
