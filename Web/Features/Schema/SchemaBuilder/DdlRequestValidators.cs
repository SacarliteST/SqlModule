using FluentValidation;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal sealed class ValidateTargetDbDdlRequestValidator : AbstractValidator<ValidateTargetDbDdlRequest>
{
    public ValidateTargetDbDdlRequestValidator()
    {
        RuleFor(x => x.DbmsId).NotNull().NotEmpty();
        RuleFor(x => x.DbName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DdlScript).NotEmpty().MaximumLength(1_000_000);
    }
}

internal sealed class CreateTargetDbFromDdlRequestValidator : AbstractValidator<CreateTargetDbFromDdlRequest>
{
    public CreateTargetDbFromDdlRequestValidator()
    {
        RuleFor(x => x.DbmsId).NotNull().NotEmpty();
        RuleFor(x => x.DbName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.IsReadOnly).NotNull();
        RuleFor(x => x.DdlScript).NotEmpty().MaximumLength(1_000_000);
    }
}
