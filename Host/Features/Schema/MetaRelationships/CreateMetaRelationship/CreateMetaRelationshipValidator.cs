using FluentValidation;
using SQLModule.Contracts.Schema.MetaRelationship;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class CreateMetaRelationshipValidator : AbstractValidator<CreateMetaRelationshipRequest>
{
    public CreateMetaRelationshipValidator()
    {
        RuleFor(x => x.RelationshipName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SourceAttributeId).NotEmpty();
        RuleFor(x => x.TargetAttributeId).NotEmpty();
        RuleFor(x => x.TargetAttributeId)
            .NotEqual(x => x.SourceAttributeId)
            .WithMessage("TargetAttributeId не может совпадать с SourceAttributeId.");
        RuleFor(x => x.DeleteRule).MaximumLength(50);
        RuleFor(x => x.UpdateRule).MaximumLength(50);
    }
}
