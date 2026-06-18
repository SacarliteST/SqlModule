using FluentValidation;
using SQLModule.Contracts.Schema.TargetDb;

namespace SQLModule.Host.Features.Schema.TargetDbs;

public sealed class CreateTargetDbValidator : AbstractValidator<CreateTargetDbRequest>
{
    public CreateTargetDbValidator()
    {
        RuleFor(x => x.DbmsId).NotEmpty();
        RuleFor(x => x.DbName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
