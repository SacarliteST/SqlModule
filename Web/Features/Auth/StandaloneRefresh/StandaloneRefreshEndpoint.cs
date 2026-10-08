using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using SQLModule.Contracts;
using SQLModule.Contracts.Auth;
using SQLModule.Application.Auth;
using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Auth.StandaloneRefresh;

internal sealed class StandaloneRefreshEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Refresh, Handle)
            .AllowAnonymous()
            .WithName("StandaloneRefresh")
            .WithTags("Auth")
            .WithSummary("Standalone-обновление токена")
            .WithDescription(
                "Тонкий прокси: ротирует refresh-токен в IdentityService и сразу обменивает новый " +
                "access-токен на audience SqlModule. Refresh-токен одноразовый: в ответе приходит новый, " +
                "старый больше не действует.")
            .Produces<StandaloneLoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<StandaloneRefreshRequest>>();
    }

    private static async Task<IResult> Handle(
        StandaloneRefreshRequest request,
        ISender sender,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var response = await sender.Send<StandaloneRefreshCommand, Result<StandaloneAuthResult>>(
            new StandaloneRefreshCommand(request.RefreshToken, StandaloneAuthAudience.Resolve(configuration)), ct);
        var result = response.Value!;

        return result.Outcome switch
        {
            StandaloneAuthOutcome.Success => TypedResults.Ok(
                new StandaloneLoginResponse(result.AccessToken!, result.ExpiresIn, result.RefreshToken ?? String.Empty)),
            StandaloneAuthOutcome.InvalidCredentials => ApiProblemFactory.ToResult(
                StatusCodes.Status401Unauthorized,
                "Сессия истекла",
                "Refresh-токен недействителен или истёк. Войдите заново.",
                "Auth.InvalidRefreshToken"),
            _ => ApiProblemFactory.ToResult(
                StatusCodes.Status503ServiceUnavailable,
                "Сервис временно недоступен",
                "IdentityService недоступен. Попробуйте ещё раз позже.",
                "Auth.IdentityUnavailable"),
        };
    }
}
