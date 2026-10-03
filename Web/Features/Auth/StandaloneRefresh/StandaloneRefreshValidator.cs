using FluentValidation;
using SQLModule.Contracts.Auth;

namespace SQLModule.Web.Features.Auth.StandaloneRefresh;

internal sealed class StandaloneRefreshValidator : AbstractValidator<StandaloneRefreshRequest>
{
    public StandaloneRefreshValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}
