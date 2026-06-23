using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal sealed class CreateSqlTaskValidator : AbstractValidator<CreateSqlTaskRequest>
{
    public CreateSqlTaskValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
        RuleFor(x => x.SqlQueryId).NotEmpty();
        RuleFor(x => x.TaskName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaskText).NotEmpty();
        RuleFor(x => x.DifficultyLevel).InclusiveBetween((short)1, (short)5);
    }
}
