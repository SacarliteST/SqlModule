using FluentValidation;
using SQLModule.Contracts.DbmsCatalog.PhysicalType;

namespace SQLModule.Host.Features.DbmsCatalog.PhysicalTypes;

internal sealed class UpdatePhysicalTypeValidator : AbstractValidator<UpdatePhysicalTypeRequest>
{
    public UpdatePhysicalTypeValidator()
    {
        RuleFor(x => x.TypeName).NotEmpty().MaximumLength(100);
    }
}
