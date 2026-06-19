using System.Security.Claims;
using SQLModule.Domain;
using SQLModule.Domain.Common;

namespace SQLModule.Host.Common;

/// <summary>
/// Реализация <see cref="ICurrentUser"/> через <see cref="IHttpContextAccessor"/>.
/// Извлекает <see cref="UserId"/> из claim <see cref="ClaimTypes.NameIdentifier"/> текущего запроса.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    /// <summary>
    /// Идентификатор аутентифицированного пользователя или <see langword="null"/>,
    /// если запрос анонимный или claim отсутствует.
    /// </summary>
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
