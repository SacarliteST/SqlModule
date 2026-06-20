using FluentValidation;
using SQLModule.Contracts.Schema.MetaAttribute;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal sealed class CreateMetaAttributeValidator : AbstractValidator<CreateMetaAttributeRequest>
{
    public CreateMetaAttributeValidator()
    {
        RuleFor(x => x.MetaTableId).NotEmpty();
        RuleFor(x => x.PhysicalTypeId).NotEmpty();
        RuleFor(x => x.AttributeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
    }
}
