using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal sealed class GetAllPhysicalTypesValidator : AbstractValidator<GetAllPhysicalTypesRequest>
{
    public GetAllPhysicalTypesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
