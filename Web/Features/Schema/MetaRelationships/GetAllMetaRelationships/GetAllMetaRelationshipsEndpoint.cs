using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.MetaRelationship;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.MetaRelationships;

internal sealed class GetAllMetaRelationshipsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.MetaRelationships.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllMetaRelationships")
            .WithTags("Schema")
            .WithSummary("Получить список связей")
            .WithDescription(
                "Возвращает постраничный список FK-связей. " +
                "Параметры: offset (≥0), limit (1–100), attributeId (опц. — вернуть связи, " +
                "где атрибут является Source или Target). " +
                "400 — не прошла валидация пагинации.")
            .Produces<PageResponse<MetaRelationshipResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
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
