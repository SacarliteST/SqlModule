using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
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
        IConfiguration configuration,
        CancellationToken ct)
    {
        // AuthOptions намеренно не зарегистрирован как IOptions<AuthOptions> — WebExtensions.AddWeb
        // читает его в локальную переменную только для настройки JwtBearer. Читаем ту же секцию
        // напрямую, чтобы всегда обменивать на ту же audience, которую сам SqlModule валидирует.
        var audience = configuration.GetSection(AuthOptions.SectionKey)[nameof(AuthOptions.Audience)] ?? String.Empty;
        var result = await identityAuthClient.LoginAndExchangeAsync(
            request.Email, request.Password, audience, ct);

        return result.Outcome switch
        {
            IdentityLoginOutcome.Success => TypedResults.Ok(
                new StandaloneLoginResponse(result.AccessToken!, result.ExpiresIn)),
            IdentityLoginOutcome.InvalidCredentials => ApiProblemFactory.ToResult(
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
