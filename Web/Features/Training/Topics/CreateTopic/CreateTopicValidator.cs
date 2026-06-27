using FluentValidation;
using SQLModule.Contracts.Training.Topic;

namespace SQLModule.Web.Features.Training.Topics;

internal sealed class CreateTopicValidator : AbstractValidator<CreateTopicRequest>
{
    public CreateTopicValidator()
    {
        RuleFor(x => x.TopicName).NotEmpty().MaximumLength(300);
    }
}
