using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal sealed class CreateMetaAttributeEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.MetaAttributes.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateMetaAttribute")
            .WithTags("Schema", "DevTools")
            .WithSummary("Создать мета-атрибут (колонку)")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Создаёт мета-атрибут (колонку) в указанной мета-таблице. " +
                "Возвращает 201 Created с телом ответа. " +
                "422 — не прошла валидация (пустое имя, пустые Id, SortOrder < 0). " +
                "409 — MetaTable или PhysicalType с указанным Id не найден.")
            .Produces<MetaAttributeResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateMetaAttributeRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateMetaAttributeRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateMetaAttributeCommand, Result<MetaAttributeResponse>>(
            MetaAttributeMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.MetaAttributes.ForId(r.Id));
    }
}
