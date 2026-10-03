namespace SQLModule.Contracts.Auth;

/// <summary>Refresh-токен, выданный при standalone-входе или предыдущем обновлении.</summary>
public sealed record StandaloneRefreshRequest(string RefreshToken);
