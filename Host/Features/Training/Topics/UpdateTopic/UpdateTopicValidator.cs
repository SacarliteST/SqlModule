using FluentValidation;
using SQLModule.Contracts.Training.Topic;

namespace SQLModule.Host.Features.Training.Topics;

internal sealed class UpdateTopicValidator : AbstractValidator<UpdateTopicRequest>
{
    public UpdateTopicValidator()
    {
        RuleFor(x => x.TopicName).NotEmpty().MaximumLength(300);
    }
}
