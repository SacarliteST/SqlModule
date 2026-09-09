using FluentValidation;
using SQLModule.Contracts.Training.Topic;

namespace SQLModule.Web.Features.Training.Topics;

internal sealed class UpdateTopicValidator : AbstractValidator<UpdateTopicRequest>
{
    public UpdateTopicValidator()
    {
        RuleFor(x => x.TopicName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
