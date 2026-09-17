namespace SQLModule.Contracts.Auth;

/// <summary>Токен, уже обменянный на audience SqlModule — готов к использованию против его API.</summary>
public sealed record StandaloneLoginResponse(string AccessToken, int ExpiresIn);
