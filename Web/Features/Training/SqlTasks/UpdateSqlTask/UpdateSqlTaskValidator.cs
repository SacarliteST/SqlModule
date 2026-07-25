using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed class UpdateSqlTaskValidator : AbstractValidator<UpdateSqlTaskRequest>
{
    public UpdateSqlTaskValidator()
    {
        RuleFor(x => x.TaskName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaskText).NotEmpty();
        RuleFor(x => x.DifficultyLevel).InclusiveBetween((short)1, (short)5);
        RuleFor(x => x.PublicationStatus!.Value)
            .IsInEnum()
            .When(x => x.PublicationStatus.HasValue);
        RuleFor(x => x.TopicId!.Value)
            .NotEmpty()
            .When(x => x.TopicId.HasValue);
        RuleFor(x => x.SqlQueryId!.Value)
            .NotEmpty()
            .When(x => x.SqlQueryId.HasValue);
    }
}
