using FluentValidation;
using SQLModule.Contracts.Auth;

namespace SQLModule.Web.Features.Auth.StandaloneLogin;

internal sealed class StandaloneLoginValidator : AbstractValidator<StandaloneLoginRequest>
{
    public StandaloneLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
