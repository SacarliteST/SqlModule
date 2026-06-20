using FluentValidation;
using SQLModule.Contracts.Schema.MetaRelationship;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class UpdateMetaRelationshipValidator : AbstractValidator<UpdateMetaRelationshipRequest>
{
    public UpdateMetaRelationshipValidator()
    {
        RuleFor(x => x.RelationshipName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DeleteRule).MaximumLength(50);
        RuleFor(x => x.UpdateRule).MaximumLength(50);
    }
}
