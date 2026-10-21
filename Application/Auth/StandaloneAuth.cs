using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;

namespace SQLModule.Application.Auth;

/// <summary>Итог standalone-операции аутентификации.</summary>
public enum StandaloneAuthOutcome
{
    Success,
    InvalidCredentials,
    Unavailable,
}

/// <summary>Результат входа или обновления токена; токен уже обменян на нужную audience.</summary>
public sealed record StandaloneAuthResult(
    StandaloneAuthOutcome Outcome,
    string? AccessToken,
    int ExpiresIn,
    string? RefreshToken = null,
    string? ErrorTitle = null,
    string? ErrorDetail = null);

/// <summary>Порт провайдера идентификации для standalone-входа; реализуется в Host поверх клиента IdentityService.</summary>
public interface IStandaloneAuthGateway
{
    Task<StandaloneAuthResult> LoginAsync(string email, string password, string audience, CancellationToken ct);

    Task<StandaloneAuthResult> RefreshAsync(string refreshToken, string audience, CancellationToken ct);

    Task LogoutAsync(string refreshToken, CancellationToken ct);
}

public record StandaloneLoginCommand(string Email, string Password, string Audience)
    : IRequest<Result<StandaloneAuthResult>>;

public record StandaloneRefreshCommand(string RefreshToken, string Audience)
    : IRequest<Result<StandaloneAuthResult>>;

public record StandaloneLogoutCommand(string RefreshToken) : IRequest<Result>;

public sealed class StandaloneLoginHandler(IStandaloneAuthGateway gateway)
    : IRequestHandler<StandaloneLoginCommand, Result<StandaloneAuthResult>>
{
    public async Task<Result<StandaloneAuthResult>> Handle(StandaloneLoginCommand command, CancellationToken ct) =>
        await gateway.LoginAsync(command.Email, command.Password, command.Audience, ct);
}

public sealed class StandaloneRefreshHandler(IStandaloneAuthGateway gateway)
    : IRequestHandler<StandaloneRefreshCommand, Result<StandaloneAuthResult>>
{
    public async Task<Result<StandaloneAuthResult>> Handle(StandaloneRefreshCommand command, CancellationToken ct) =>
        await gateway.RefreshAsync(command.RefreshToken, command.Audience, ct);
}

public sealed class StandaloneLogoutHandler(IStandaloneAuthGateway gateway)
    : IRequestHandler<StandaloneLogoutCommand, Result>
{
    public async Task<Result> Handle(StandaloneLogoutCommand command, CancellationToken ct)
    {
        await gateway.LogoutAsync(command.RefreshToken, ct);
        return Result.Success();
    }
}
