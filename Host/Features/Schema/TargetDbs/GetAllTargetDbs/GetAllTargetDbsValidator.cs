using FluentValidation;
using SQLModule.Contracts.Schema.TargetDb;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal sealed class GetAllTargetDbsValidator : AbstractValidator<GetAllTargetDbsRequest>
{
    public GetAllTargetDbsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
