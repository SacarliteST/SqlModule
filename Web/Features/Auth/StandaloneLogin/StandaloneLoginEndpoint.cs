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
                "остаётся на сервере — в браузер попадает только уже готовый обменянный токен " +
                "и refresh-токен для POST /auth/refresh.")
            .Produces<StandaloneLoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<StandaloneLoginRequest>>();
    }

    private static async Task<IResult> Handle(
        StandaloneLoginRequest request,
        ISender sender,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var audience = StandaloneAuthAudience.Resolve(configuration);
        var response = await sender.Send<StandaloneLoginCommand, Result<StandaloneAuthResult>>(
            new StandaloneLoginCommand(request.Email, request.Password, audience), ct);
        var result = response.Value!;

        return result.Outcome switch
        {
            StandaloneAuthOutcome.Success => TypedResults.Ok(
                new StandaloneLoginResponse(result.AccessToken!, result.ExpiresIn, result.RefreshToken ?? String.Empty)),
            StandaloneAuthOutcome.InvalidCredentials => ApiProblemFactory.ToResult(
                StatusCodes.Status401Unauthorized,
                String.IsNullOrWhiteSpace(result.ErrorTitle) ? "Не удалось войти" : result.ErrorTitle,
                String.IsNullOrWhiteSpace(result.ErrorDetail) ? "Неверный email или пароль." : result.ErrorDetail,
                "Auth.InvalidCredentials"),
            _ => ApiProblemFactory.ToResult(
                StatusCodes.Status503ServiceUnavailable,
                "Сервис временно недоступен",
                "IdentityService недоступен. Попробуйте ещё раз позже.",
                "Auth.IdentityUnavailable"),
        };
    }
}
