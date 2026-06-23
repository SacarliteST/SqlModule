using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Host.Features.Training.Attempts.SubmitAttempt;

internal sealed class SubmitAttemptValidator : AbstractValidator<SubmitAttemptRequest>
{
    public SubmitAttemptValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.SubmittedSql).NotEmpty();
    }
}
