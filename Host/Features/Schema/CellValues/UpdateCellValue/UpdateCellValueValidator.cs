using FluentValidation;
using SQLModule.Contracts.Schema.CellValue;

namespace SQLModule.Host.Features.Schema.CellValues;

internal sealed class UpdateCellValueValidator : AbstractValidator<UpdateCellValueRequest>
{
    public UpdateCellValueValidator()
    {
        RuleFor(x => x.TextValue).MaximumLength(2000);
    }
}
