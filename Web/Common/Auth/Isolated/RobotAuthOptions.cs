using Microsoft.AspNetCore.Authentication;

namespace SQLModule.Web.Common.Auth.Isolated;

public sealed class RobotAuthOptions : AuthenticationSchemeOptions
{
    public const string RobotAuthenticationScheme = nameof(RobotAuthenticationScheme);

    public const string RobotAuthenticationType = "RobotAuthentication";
}
