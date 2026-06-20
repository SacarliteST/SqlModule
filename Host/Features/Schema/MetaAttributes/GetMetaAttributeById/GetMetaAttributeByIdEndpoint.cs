using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaAttribute;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal sealed class GetMetaAttributeByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaAttributes.ById, Handle)
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
