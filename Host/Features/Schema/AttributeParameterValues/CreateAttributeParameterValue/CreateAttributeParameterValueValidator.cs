using FluentValidation;
using SQLModule.Contracts.Schema.AttributeParameterValue;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal sealed class CreateAttributeParameterValueValidator
    : AbstractValidator<CreateAttributeParameterValueRequest>
{
    public CreateAttributeParameterValueValidator()
    {
        RuleFor(x => x.MetaAttributeId).NotEmpty();
        RuleFor(x => x.ParameterDefinitionId).NotEmpty();
        RuleFor(x => x.ParameterValue).NotEmpty().MaximumLength(500);
    }
}
