using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SQLModule.Contracts;
using SQLModule.Contracts.Auth;
using SQLModule.Identity.Client;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Auth;

namespace SQLModule.Web.Features.Auth.StandaloneLogin;

internal sealed class StandaloneLoginEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Login, Handle)
            .AllowAnonymous()
            .WithName("StandaloneLogin")
            .WithTags("Auth")
            .WithSummary("Standalone-вход через SqlModule")
            .WithDescription(
                "Тонкий прокси: логинит пользователя в IdentityService и сразу обменивает " +
                "полученный токен на audience SqlModule, без session_id. Секрет клиента обмена " +
                "остаётся на сервере — в браузер попадает только уже готовый обменянный токен.")
            .Produces<StandaloneLoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<StandaloneLoginRequest>>();
    }

    private static async Task<IResult> Handle(
        StandaloneLoginRequest request,
        IIdentityAuthClient identityAuthClient,
        IOptions<AuthOptions> authOptions,
        CancellationToken ct)
    {
        var result = await identityAuthClient.LoginAndExchangeAsync(
            request.Email, request.Password, authOptions.Value.Audience, ct);

        return result.Outcome switch
        {
            IdentityLoginOutcome.Success => TypedResults.Ok(
                new StandaloneLoginResponse(result.AccessToken!, result.ExpiresIn)),
            IdentityLoginOutcome.InvalidCredentials => ApiProblemFactory.ToResult(
                StatusCodes.Status401Unauthorized,
                "Не удалось войти",
                "Неверный email или пароль.",
                "Auth.InvalidCredentials"),
            _ => ApiProblemFactory.ToResult(
                StatusCodes.Status503ServiceUnavailable,
                "Сервис временно недоступен",
                "IdentityService недоступен. Попробуйте ещё раз позже.",
                "Auth.IdentityUnavailable"),
        };
    }
}
