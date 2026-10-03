namespace SQLModule.Contracts.Auth;

/// <summary>Токен, уже обменянный на audience SqlModule — готов к использованию против его API.</summary>
/// <param name="AccessToken">Обменянный токен под audience SqlModule.</param>
/// <param name="ExpiresIn">Время жизни токена в секундах.</param>
/// <param name="RefreshToken">Refresh-токен IdentityService для <c>POST /auth/refresh</c>; одноразовый, ротируется при каждом обновлении.</param>
public sealed record StandaloneLoginResponse(string AccessToken, int ExpiresIn, string RefreshToken);
