using FluentValidation;
using SQLModule.Contracts.Schema.AttributeParameterValue;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal sealed class GetAllAttributeParameterValuesValidator
    : AbstractValidator<GetAllAttributeParameterValuesRequest>
{
    public GetAllAttributeParameterValuesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
