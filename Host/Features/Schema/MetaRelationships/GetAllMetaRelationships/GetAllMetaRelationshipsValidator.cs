using FluentValidation;
using SQLModule.Contracts.Schema.MetaRelationship;

namespace SQLModule.Host.Features.Schema.MetaRelationships;

internal sealed class GetAllMetaRelationshipsValidator : AbstractValidator<GetAllMetaRelationshipsRequest>
{
    public GetAllMetaRelationshipsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
