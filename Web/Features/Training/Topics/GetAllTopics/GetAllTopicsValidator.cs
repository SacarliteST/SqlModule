using FluentValidation;
using SQLModule.Contracts.Training.Topic;

namespace SQLModule.Web.Features.Training.Topics;

internal sealed class GetAllTopicsValidator : AbstractValidator<GetAllTopicsRequest>
{
    public GetAllTopicsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
