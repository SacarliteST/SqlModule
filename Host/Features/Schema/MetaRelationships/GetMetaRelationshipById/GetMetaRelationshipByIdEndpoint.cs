using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class GetMetaRelationshipByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaRelationships.ById, Handle)
            .WithName("GetMetaRelationshipById")
            .WithTags("Schema")
            .WithSummary("Получить связь по Id")
            .WithDescription(
                "Возвращает связь между мета-атрибутами по идентификатору. " +
                "404 — связь с указанным Id не найдена.")
            .Produces<MetaRelationshipResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetMetaRelationshipByIdQuery, Result<MetaRelationshipResponse>>(
            new GetMetaRelationshipByIdQuery(id), ct);
        return result.ToOk();
    }
}
