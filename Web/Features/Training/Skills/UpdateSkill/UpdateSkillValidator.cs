using FluentValidation;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Web.Features.Training.Skills;

internal sealed class UpdateSkillValidator : AbstractValidator<UpdateSkillRequest>
{
    public UpdateSkillValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Description).MaximumLength(2000);
    }
}
