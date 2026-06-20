using FluentValidation;
using SQLModule.Contracts.Schema.MetaAttribute;

namespace SQLModule.Host.Features.Schema.MetaAttributes;

internal sealed class UpdateMetaAttributeValidator : AbstractValidator<UpdateMetaAttributeRequest>
{
    public UpdateMetaAttributeValidator()
    {
        RuleFor(x => x.AttributeName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo((short)0);
    }
}
