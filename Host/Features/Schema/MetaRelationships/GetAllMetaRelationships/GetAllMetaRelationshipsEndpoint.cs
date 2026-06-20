using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class GetAllMetaRelationshipsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaRelationships.Collection, Handle)
            .WithName("GetAllMetaRelationships")
            .WithTags("Schema")
            .WithSummary("Получить список связей")
            .WithDescription(
                "Возвращает постраничный список FK-связей. " +
                "Параметры: offset (≥0), limit (1–100), attributeId (опц. — вернуть связи, " +
                "где атрибут является Source или Target). " +
                "400 — не прошла валидация пагинации.")
            .Produces<PageResponse<MetaRelationshipResponse>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllMetaRelationshipsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllMetaRelationshipsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllMetaRelationshipsQuery, Result<PageResponse<MetaRelationshipResponse>>>(
            new GetAllMetaRelationshipsQuery(request.Offset, request.Limit, request.AttributeId), ct);
        return result.ToOk();
    }
}
