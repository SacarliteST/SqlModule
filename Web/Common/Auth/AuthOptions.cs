namespace SQLModule.Web.Common.Auth;

/// <summary>Настройки JWT-аутентификации (секция "Auth" в конфиге).</summary>
internal sealed class AuthOptions
{
    public const string SectionKey = "Auth";

    /// <summary>URL Identity-сервиса (используется для OIDC discovery + JWKS).</summary>
    public string Authority { get; init; } = String.Empty;

    /// <summary>Ожидаемый audience токена.</summary>
    public string Audience { get; init; } = String.Empty;
}
