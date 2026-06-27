using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal sealed class UpdateMetaAttributeEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.MetaAttributes.ById, Handle)
            .WithName("UpdateMetaAttribute")
            .WithTags("Schema", "DevTools")
            .WithSummary("Обновить мета-атрибут")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Обновляет название, признаки и порядок отображения мета-атрибута. " +
                "FK-колонки (MetaTableId, PhysicalTypeId) не изменяются. " +
                "Возвращает 204 No Content. " +
                "422 — не прошла валидация. " +
                "404 — мета-атрибут с указанным Id не найден.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateMetaAttributeRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateMetaAttributeRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateMetaAttributeCommand, Result>(
            MetaAttributeMappings.ToCommand(id, request), ct);
        return result.ToNoContent();
    }
}
