using FluentValidation;
using SQLModule.Contracts.Schema.CellValue;

namespace SQLModule.Web.Features.Schema.CellValues;

internal sealed class GetAllCellValuesValidator : AbstractValidator<GetAllCellValuesRequest>
{
    public GetAllCellValuesValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
