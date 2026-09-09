using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed class UpdateSqlTaskValidator : AbstractValidator<UpdateSqlTaskRequest>
{
    public UpdateSqlTaskValidator()
    {
        RuleFor(x => x.TaskName).NotNull().NotEmpty().MaximumLength(300);
        RuleFor(x => x.TaskText).NotNull().NotEmpty();
        RuleFor(x => x.DifficultyLevel).NotNull().InclusiveBetween((short)1, (short)5);
        RuleFor(x => x.PublicationStatus)
            .IsInEnum()
            .When(x => x.PublicationStatus.HasValue);
        RuleFor(x => x.TopicId)
            .Must(topicId => !topicId.HasValue || topicId.Value != Guid.Empty)
            .WithMessage("'Topic Id' не должен быть пустым.");
    }
}
