using FluentValidation;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Web.Features.Training.Skills;

internal sealed class GetSkillsValidator : AbstractValidator<GetSkillsRequest>
{
    public GetSkillsValidator()
    {
        RuleFor(request => request.Search).MaximumLength(200);
        RuleFor(request => request.Offset).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Limit).InclusiveBetween(1, 100);
        RuleFor(request => request.Origin)
            .IsInEnum()
            .When(request => request.Origin.HasValue);
    }
}
