namespace SQLModule.Contracts.Auth;

/// <summary>Учётные данные для standalone-входа.</summary>
public sealed record StandaloneLoginRequest(string Email, string Password);
