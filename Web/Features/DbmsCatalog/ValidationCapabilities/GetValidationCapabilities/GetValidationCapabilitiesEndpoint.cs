using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.Validation;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.ValidationCapabilities.GetValidationCapabilities;

internal sealed class GetValidationCapabilitiesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.ValidationCapabilities.ByDbmsId, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetDbmsValidationCapabilities")
            .WithTags("DbmsCatalog")
            .Produces<DbmsValidationCapabilitiesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> Handle(Guid dbmsId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<
            GetValidationCapabilitiesQuery,
            Result<DbmsValidationCapabilitiesResponse>>(
            new GetValidationCapabilitiesQuery(dbmsId),
            ct);

        return result.ToOk();
    }
}
