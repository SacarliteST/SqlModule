using System.Security.Claims;

namespace SQLModule.Web.Common.Auth.Isolated;

/// <summary>
/// Предоставляет Identity пользователя robot@scoodle.local.
/// Содержит все роли — робот может всё в UI при изолированном запуске.
/// Никогда не включать в проде.
/// </summary>
internal static class RobotIdentity
{
    /// <summary>Фиксированный Guid робота — стабильный userId для аудита и попыток.</summary>
    public static readonly Guid RobotUserId = new("00000000-0000-0000-0000-0000000000AA");

    public static ClaimsIdentity ClaimsIdentity { get; } = new(
        new Claim[]
        {
            new(ClaimTypes.NameIdentifier, RobotUserId.ToString()),
            new(ClaimTypes.Name, "robot@scoodle.local"),
            new(ClaimTypes.Email, "robot@scoodle.local"),
            new(ClaimTypes.Role, Roles.Teacher),
            new(ClaimTypes.Role, Roles.Admin),
            new(ClaimTypes.Role, Roles.Student),
        },
        RobotAuthOptions.RobotAuthenticationType);
}
