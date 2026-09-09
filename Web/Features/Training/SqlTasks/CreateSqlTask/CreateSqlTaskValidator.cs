using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed class CreateSqlTaskValidator : AbstractValidator<CreateSqlTaskRequest>
{
    public CreateSqlTaskValidator()
    {
        RuleFor(x => x.TopicId).NotNull().NotEmpty();
        RuleFor(x => x.TaskName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaskText).NotEmpty();
        RuleFor(x => x.DifficultyLevel)
            .NotNull()
            .InclusiveBetween((short)1, (short)5);
        RuleFor(x => x.ReferenceQuery)
            .NotNull()
            .SetValidator(new ReferenceQueryRequestValidator()!);
    }
}
