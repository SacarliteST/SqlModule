using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.PhysicalTypes;

internal sealed class UpdatePhysicalTypeEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.PhysicalTypes.ById, Handle)
            .WithName("UpdatePhysicalType")
            .WithTags("DbmsCatalog")
            .WithSummary("Обновить физический тип данных")
            .WithDescription(
                "Обновляет название физического типа данных. " +
                "Возвращает 204 No Content. " +
                "422 — не прошла валидация (пустое имя или > 100 символов). " +
                "404 — физический тип с указанным Id не найден.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdatePhysicalTypeRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdatePhysicalTypeRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdatePhysicalTypeCommand, Result>(
            PhysicalTypeMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
