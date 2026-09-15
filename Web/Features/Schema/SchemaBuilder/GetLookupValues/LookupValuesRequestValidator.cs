using FluentValidation;
using SQLModule.Contracts.Schema.SchemaBuilder;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetLookupValues;

internal sealed class LookupValuesRequestValidator : AbstractValidator<LookupValuesRequest>
{
    public LookupValuesRequestValidator()
    {
        RuleFor(request => request.ValueColumnId).NotNull().NotEmpty();
        RuleFor(request => request.LabelColumnId).NotEmpty().When(request => request.LabelColumnId.HasValue);
        RuleFor(request => request.Search).MaximumLength(200);
        RuleFor(request => request.Offset)
            .GreaterThanOrEqualTo(0)
            .When(request => request.Offset.HasValue)
            .WithMessage("offset должен быть больше или равен 0.");
        RuleFor(request => request.Limit)
            .InclusiveBetween(1, 100)
            .When(request => request.Limit.HasValue)
            .WithMessage("limit должен находиться в диапазоне от 1 до 100.");
    }
}
