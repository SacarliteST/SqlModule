using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Host.Features.Training.Attempts;

internal sealed class GetAllAttemptsValidator : AbstractValidator<GetAllAttemptsRequest>
{
    public GetAllAttemptsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
