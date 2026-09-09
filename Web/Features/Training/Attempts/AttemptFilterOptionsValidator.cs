using FluentValidation;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed class AttemptFilterOptionsValidator : AbstractValidator<AttemptFilterOptionsRequest>
{
    public AttemptFilterOptionsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).Must(HaveValidSearchLength)
            .WithErrorCode("MaximumLength");
        RuleFor(x => x.Id).Must(BeGuid).When(x => x.Id is not null)
            .WithErrorCode("InvalidFormat");
    }

    private static bool BeGuid(string? value) => Guid.TryParse(value, out _);
    private static bool HaveValidSearchLength(string? value)
        => String.IsNullOrWhiteSpace(value) || value.Trim().Length <= 200;
}

internal sealed class AttemptTaskFilterOptionsValidator : AbstractValidator<AttemptTaskFilterOptionsRequest>
{
    public AttemptTaskFilterOptionsValidator()
    {
        RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).Must(HaveValidSearchLength)
            .WithErrorCode("MaximumLength");
        RuleFor(x => x.Id).Must(BeGuid).When(x => x.Id is not null)
            .WithErrorCode("InvalidFormat");
        RuleFor(x => x.TopicId).Must(BeGuid).When(x => x.TopicId is not null)
            .WithErrorCode("InvalidFormat");
    }

    private static bool BeGuid(string? value) => Guid.TryParse(value, out _);
    private static bool HaveValidSearchLength(string? value)
        => String.IsNullOrWhiteSpace(value) || value.Trim().Length <= 200;
}
