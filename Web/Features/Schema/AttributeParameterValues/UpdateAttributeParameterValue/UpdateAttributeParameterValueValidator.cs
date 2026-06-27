using FluentValidation;
using SQLModule.Contracts.Schema.AttributeParameterValue;

namespace SQLModule.Web.Features.Schema.AttributeParameterValues;

internal sealed class UpdateAttributeParameterValueValidator
    : AbstractValidator<UpdateAttributeParameterValueRequest>
{
    public UpdateAttributeParameterValueValidator()
    {
        RuleFor(x => x.ParameterValue).NotEmpty().MaximumLength(500);
    }
}
