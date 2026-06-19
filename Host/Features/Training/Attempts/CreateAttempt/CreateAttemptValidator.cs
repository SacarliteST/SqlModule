using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Host.Features.Training.Attempts;

internal sealed class CreateAttemptValidator : AbstractValidator<CreateAttemptRequest>
{
    public CreateAttemptValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.QueryId).NotEmpty();
        RuleFor(x => x.EndAttempt)
            .Must((req, end) => end >= req.StartAttempt)
            .WithMessage("Время завершения попытки не может быть раньше времени начала.");
    }
}
