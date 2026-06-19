using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Host.Features.Training.Attempts;

internal sealed class UpdateAttemptValidator : AbstractValidator<UpdateAttemptRequest>
{
    public UpdateAttemptValidator()
    {
        RuleFor(x => x.EndAttempt).NotEqual(default(DateTimeOffset));
    }
}
