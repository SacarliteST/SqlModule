using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

internal sealed class GetAllPhysicalTypesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.PhysicalTypes.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllPhysicalTypes")
            .WithTags("DbmsCatalog")
            .WithSummary("Получить список физических типов данных")
            .WithDescription(
                "Возвращает постраничный список физических типов данных. " +
                "Параметры: offset (≥ 0), limit (1–100), dbmsId (опц. — фильтрация по СУБД). " +
                "Результат упорядочен по TypeName. " +
                "422 — не прошла валидация пагинации.")
            .Produces<PageResponse<PhysicalTypeResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllPhysicalTypesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllPhysicalTypesRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllPhysicalTypesQuery, Result<PageResponse<PhysicalTypeResponse>>>(
            new GetAllPhysicalTypesQuery(request.Offset, request.Limit, request.DbmsId), ct);
        return result.ToOk();
    }
}
