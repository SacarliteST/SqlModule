using FluentValidation;
using SQLModule.Contracts.Schema.MetaAttribute;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal sealed class GetAllMetaAttributesValidator : AbstractValidator<GetAllMetaAttributesRequest>
{
    public GetAllMetaAttributesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
