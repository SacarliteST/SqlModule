using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Contracts;
using SQLModule.Contracts.Auth;
using SQLModule.Application.Auth;
using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Web.Common;
using SQLModule.Web.Features.Auth.StandaloneRefresh;

namespace SQLModule.Web.Features.Auth.StandaloneLogout;

internal sealed class StandaloneLogoutEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Logout, Handle)
            .AllowAnonymous()
            .WithName("StandaloneLogout")
            .WithTags("Auth")
            .WithSummary("Standalone-выход")
            .WithDescription("Отзывает refresh-токен в IdentityService. Идемпотентно: всегда 204.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<StandaloneRefreshRequest>>();
    }

    private static async Task<IResult> Handle(
        StandaloneRefreshRequest request,
        ISender sender,
        CancellationToken ct)
    {
        await sender.Send<StandaloneLogoutCommand, Result>(new StandaloneLogoutCommand(request.RefreshToken), ct);
        return TypedResults.NoContent();
    }
}
