namespace SQLModule.Identity.Client;

/// <summary>Итог standalone-логина через IdentityService.</summary>
public enum IdentityLoginOutcome
{
    /// <summary>Логин и обмен прошли успешно.</summary>
    Success,

    /// <summary>IdentityService отклонил учётные данные.</summary>
    InvalidCredentials,

    /// <summary>IdentityService недоступен или вернул неожиданный ответ.</summary>
    Unavailable,
}

/// <param name="Outcome">Итог операции.</param>
/// <param name="AccessToken">Токен, уже обменянный на целевой audience. <see langword="null"/> при ошибке.</param>
/// <param name="ExpiresIn">Время жизни токена в секундах.</param>
/// <param name="ErrorTitle">
/// Заголовок ошибки IdentityService при <see cref="IdentityLoginOutcome.InvalidCredentials"/> —
/// например, различает неверный пароль и заблокированную учётную запись.
/// </param>
/// <param name="ErrorDetail">Подробное сообщение ошибки IdentityService.</param>
public sealed record IdentityLoginResult(
    IdentityLoginOutcome Outcome,
    string? AccessToken,
    int ExpiresIn,
    string? ErrorTitle = null,
    string? ErrorDetail = null);

/// <summary>
/// Логинит пользователя в IdentityService по email/паролю и сразу обменивает выданный токен
/// на токен под целевую audience — без session_id, тот же паттерн, что
/// Education использует для authoring-ссылок преподавателя.
/// </summary>
public interface IIdentityAuthClient
{
    Task<IdentityLoginResult> LoginAndExchangeAsync(
        string email,
        string password,
        string audience,
        CancellationToken ct = default);
}
