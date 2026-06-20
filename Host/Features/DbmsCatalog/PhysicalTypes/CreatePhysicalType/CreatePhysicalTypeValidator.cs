using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal sealed class CreatePhysicalTypeValidator : AbstractValidator<CreatePhysicalTypeRequest>
{
    public CreatePhysicalTypeValidator()
    {
        RuleFor(x => x.DbmsId).NotEmpty();
        RuleFor(x => x.TypeName).NotEmpty().MaximumLength(100);
    }
}
