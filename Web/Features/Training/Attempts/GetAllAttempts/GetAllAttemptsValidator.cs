using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed class GetAllAttemptsValidator : AbstractValidator<GetAllAttemptsRequest>
{
    public GetAllAttemptsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.FinalizationReason).IsInEnum().When(x => x.FinalizationReason.HasValue);
        RuleFor(x => x.ScoreFrom).InclusiveBetween(0, 100).When(x => x.ScoreFrom.HasValue);
        RuleFor(x => x.ScoreTo).InclusiveBetween(0, 100).When(x => x.ScoreTo.HasValue);
        RuleFor(x => x).Must(x => !x.ScoreFrom.HasValue || !x.ScoreTo.HasValue || x.ScoreFrom <= x.ScoreTo)
            .WithMessage("scoreFrom не может быть больше scoreTo.");
        RuleFor(x => x).Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("dateFrom не может быть позже dateTo.");
    }
}
