using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class GetAllParameterDefinitionsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.ParameterDefinitions.Collection, Handle)
            .WithName("GetAllParameterDefinitions")
            .WithTags("DbmsCatalog")
            .WithSummary("Получить список определений параметров")
            .WithDescription(
                "Возвращает постраничный список определений параметров физических типов данных. " +
                "Параметры: offset (≥ 0), limit (1–100), physicalTypeId (опц. — фильтрация по типу). " +
                "Результат упорядочен по SortOrder, затем по ParameterKey. " +
                "422 — не прошла валидация пагинации.")
            .Produces<PageResponse<ParameterDefinitionResponse>>()
            .ProducesValidationProblem()
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
