using FluentValidation;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal sealed class BatchTableRowsValidator : AbstractValidator<BatchTableRowsRequest>
{
    public BatchTableRowsValidator()
    {
        RuleFor(x => x.SchemaVersion).NotEmpty();
        RuleFor(x => x.Changes).NotNull().NotEmpty().Must(x => x is null || x.Count <= 100)
            .WithMessage("За один запрос можно изменить не более 100 строк.");
        RuleForEach(x => x.Changes!).ChildRules(change =>
        {
            change.RuleFor(x => x.Operation).NotNull().IsInEnum();
            change.RuleFor(x => x.TempId).NotEmpty()
                .When(x => x.Operation == TableRowOperation.Create);
            change.RuleFor(x => x.Id).NotNull().NotEmpty()
                .When(x => x.Operation is TableRowOperation.Update or TableRowOperation.Delete);
            change.RuleFor(x => x.Version).NotEmpty()
                .When(x => x.Operation is TableRowOperation.Update or TableRowOperation.Delete);
            change.RuleFor(x => x.SortOrder).NotNull().GreaterThanOrEqualTo(0)
                .When(x => x.Operation is TableRowOperation.Create or TableRowOperation.Update);
            change.RuleFor(x => x.Cells).NotNull()
                .When(x => x.Operation is TableRowOperation.Create or TableRowOperation.Update);
            change.RuleForEach(x => x.Cells!).ChildRules(cell =>
            {
                cell.RuleFor(x => x.Value.IsNull).NotNull();
                cell.RuleFor(x => x.Value)
                    .Must(x => x is null || !x.IsNull.GetValueOrDefault() || x.Value is null)
                    .WithMessage("Для SQL NULL поле value должно быть null.");
            });
        });
    }
}
