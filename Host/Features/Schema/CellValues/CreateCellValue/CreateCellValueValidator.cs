using FluentValidation;
using SQLModule.Contracts.Schema.CellValue;

namespace SQLModule.Host.Features.Schema.CellValues;

internal sealed class CreateCellValueValidator : AbstractValidator<CreateCellValueRequest>
{
    public CreateCellValueValidator()
    {
        RuleFor(x => x.DataRecordId).NotEmpty();
        RuleFor(x => x.MetaAttributeId).NotEmpty();
        RuleFor(x => x.TextValue).MaximumLength(2000);
    }
}
