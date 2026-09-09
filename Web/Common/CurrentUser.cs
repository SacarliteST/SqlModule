using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SQLModule.Domain;
using SQLModule.Domain.Common;
using SQLModule.Web.Common.Auth;

namespace SQLModule.Web.Common;

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

    /// <summary>Идентификатор платформенной сессии из доверенного module token.</summary>
    public Guid? ModuleSessionId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(AuthClaimNames.ModuleSessionId), out var id) ? id : null;

    /// <summary>Отображаемое имя из JWT claim <c>name</c>.</summary>
    public string? DisplayName
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            var value = principal?.FindFirstValue("name")
                        ?? principal?.FindFirstValue(ClaimTypes.Name);
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    /// <summary>Email из JWT, если Identity-сервис передал claim.</summary>
    public string? Email
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
                        ?? accessor.HttpContext?.User.FindFirstValue("email");
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
