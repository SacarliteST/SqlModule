using FluentValidation;
using SQLModule.Contracts.Schema.TargetDb;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class UpdateTargetDbValidator : AbstractValidator<UpdateTargetDbRequest>
{
    public UpdateTargetDbValidator()
    {
        RuleFor(x => x.DbName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
