using Microsoft.AspNetCore.Authorization;

namespace SQLModule.Web.Common.Auth;

/// <summary>
/// Teacher/Admin API недоступен токену платформенной студенческой сессии,
/// даже если в нём по ошибке или из-за обмена оказалась роль автора контента.
/// </summary>
internal sealed class NoPlatformSessionRequirement : IAuthorizationRequirement;

internal sealed class NoPlatformSessionHandler : AuthorizationHandler<NoPlatformSessionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NoPlatformSessionRequirement requirement)
    {
        if (!context.User.HasClaim(claim => claim.Type == AuthClaimNames.ModuleSessionId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
