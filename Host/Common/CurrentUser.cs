using System.Security.Claims;
using SQLModule.Domain;
using SQLModule.Domain.Common;

namespace SQLModule.Host.Common;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
