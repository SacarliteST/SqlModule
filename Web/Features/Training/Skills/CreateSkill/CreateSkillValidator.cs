using FluentValidation;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Web.Features.Training.Skills;

internal sealed class CreateSkillValidator : AbstractValidator<CreateSkillRequest>
{
    public CreateSkillValidator()
    {
        RuleFor(request => request.SkillId)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9._-]+$")
            .WithMessage("SkillId может содержать только строчные латинские буквы, цифры, '.', '_' и '-'.");
        RuleFor(request => request.Name).NotEmpty().MaximumLength(300);
        RuleFor(request => request.Description).MaximumLength(2000);
    }
}
