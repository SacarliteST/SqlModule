namespace SQLModule.Identity.Client;

/// <summary>
/// Доверенный клиент SqlModule для standalone-логина: логинит пользователя в IdentityService
/// от его имени и сразу обменивает токен на audience SqlModule. Секрет никогда не попадает
/// в браузер — используется только server-to-server, симметрично клиенту Education.
/// </summary>
public sealed class IdentityAuthClientOptions
{
    public const string SectionKey = "StandaloneAuth";

    public string ClientId { get; init; } = String.Empty;
    public string ClientSecret { get; init; } = String.Empty;
}
