using FluentValidation;
using SQLModule.Contracts.Schema.DataRecord;

namespace SQLModule.Web.Features.Schema.DataRecords;

internal sealed class CreateDataRecordValidator : AbstractValidator<CreateDataRecordRequest>
{
    public CreateDataRecordValidator()
    {
        RuleFor(x => x.MetaTableId).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).When(x => x.SortOrder.HasValue);
    }
}
