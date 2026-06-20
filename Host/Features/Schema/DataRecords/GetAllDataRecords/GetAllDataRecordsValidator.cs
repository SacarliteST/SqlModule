using FluentValidation;
using SQLModule.Contracts.Schema.DataRecord;

namespace SQLModule.Host.Features.Schema.DataRecords;

internal sealed class GetAllDataRecordsValidator : AbstractValidator<GetAllDataRecordsRequest>
{
    public GetAllDataRecordsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
