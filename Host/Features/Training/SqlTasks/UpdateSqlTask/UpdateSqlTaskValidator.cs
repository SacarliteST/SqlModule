using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal sealed class UpdateSqlTaskValidator : AbstractValidator<UpdateSqlTaskRequest>
{
    public UpdateSqlTaskValidator()
    {
        RuleFor(x => x.TaskName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaskText).NotEmpty();
        RuleFor(x => x.DifficultyLevel).InclusiveBetween((short)1, (short)5);
    }
}
