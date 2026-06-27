using FluentValidation;
using SQLModule.Contracts.Schema.DataRecord;

namespace SQLModule.Web.Features.Schema.DataRecords;

internal sealed class UpdateDataRecordValidator : AbstractValidator<UpdateDataRecordRequest>
{
    public UpdateDataRecordValidator()
    {
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).When(x => x.SortOrder.HasValue);
    }
}
