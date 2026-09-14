using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaAttributes;

internal sealed class GetMetaAttributeByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaAttributes.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetMetaAttributeById")
            .WithTags("Schema")
            .WithSummary("Получить мета-атрибут по Id")
            .WithDescription(
                "Возвращает мета-атрибут (колонку) по идентификатору. " +
                "404 — мета-атрибут с указанным Id не найден.")
            .Produces<MetaAttributeResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetMetaAttributeByIdQuery, Result<MetaAttributeResponse>>(
            new GetMetaAttributeByIdQuery(id), ct);
        return result.ToOk();
    }
}
