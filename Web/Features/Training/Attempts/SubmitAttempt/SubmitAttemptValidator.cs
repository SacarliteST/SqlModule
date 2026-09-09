using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Web.Features.Training.Attempts.SubmitAttempt;

internal sealed class SubmitAttemptValidator : AbstractValidator<SubmitAttemptRequest>
{
    public SubmitAttemptValidator(Microsoft.Extensions.Options.IOptions<SQLModule.Sandbox.SandboxOptions> options)
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.SubmittedSql).NotEmpty().MaximumLength(options.Value.MaxSqlLength);
    }
}
