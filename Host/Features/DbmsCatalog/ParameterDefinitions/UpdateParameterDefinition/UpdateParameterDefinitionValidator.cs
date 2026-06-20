using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

internal sealed class UpdateParameterDefinitionValidator : AbstractValidator<UpdateParameterDefinitionRequest>
{
    public UpdateParameterDefinitionValidator()
    {
        RuleFor(x => x.ParameterKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InputType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DefaultValue).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
        RuleFor(x => x.SqlFragment).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ValuePrefix).MaximumLength(50);
        RuleFor(x => x.ValueSuffix).MaximumLength(50);
        RuleFor(x => x.Separator).MaximumLength(10);
    }
}
