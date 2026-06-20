using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal sealed class GetPhysicalTypeByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.PhysicalTypes.ById, Handle)
            .WithName("GetPhysicalTypeById")
            .WithTags("DbmsCatalog")
            .WithSummary("Получить физический тип данных по Id")
            .WithDescription(
                "Возвращает физический тип данных по идентификатору. " +
                "404 — физический тип с указанным Id не найден.")
            .Produces<PhysicalTypeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetPhysicalTypeByIdQuery, Result<PhysicalTypeResponse>>(
            new GetPhysicalTypeByIdQuery(id), ct);
        return result.ToOk();
    }
}
