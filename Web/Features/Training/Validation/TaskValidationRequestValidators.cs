using FluentValidation;
using SQLModule.Contracts.Training.Validation;

namespace SQLModule.Web.Features.Training.Validation;

internal sealed class TaskValidationConfigurationRequestValidator
    : AbstractValidator<TaskValidationConfigurationRequest>
{
    public TaskValidationConfigurationRequestValidator()
    {
        RuleFor(request => request.Version)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(BeGuid)
            .WithMessage("Версия конфигурации должна быть UUID.");
        RuleFor(request => request.PassingScore).NotNull().InclusiveBetween(1, 100);
        RuleFor(request => request.MaxAttempts)
            .GreaterThan(0)
            .When(request => request.MaxAttempts.HasValue);
        RuleFor(request => request.VisibleHintGroups).NotNull();
        RuleForEach(request => request.VisibleHintGroups!)
            .IsInEnum()
            .When(request => request.VisibleHintGroups is not null);
        RuleFor(request => request.Checks).NotNull().NotEmpty();
        RuleForEach(request => request.Checks!)
            .SetValidator(new ValidationCheckRequestValidator())
            .When(request => request.Checks is not null);
    }

    private static bool BeGuid(string? value) => Guid.TryParseExact(value, "D", out _);

}

internal sealed class TaskValidationPreviewRequestValidator : AbstractValidator<TaskValidationPreviewRequest>
{
    public TaskValidationPreviewRequestValidator()
    {
        RuleFor(request => request.PassingScore).NotNull().InclusiveBetween(1, 100);
        RuleFor(request => request.MaxAttempts)
            .GreaterThan(0)
            .When(request => request.MaxAttempts.HasValue);
        RuleFor(request => request.VisibleHintGroups).NotNull();
        RuleForEach(request => request.VisibleHintGroups!)
            .IsInEnum()
            .When(request => request.VisibleHintGroups is not null);
        RuleFor(request => request.Checks).NotNull().NotEmpty();
        RuleForEach(request => request.Checks!)
            .SetValidator(new ValidationCheckPreviewRequestValidator())
            .When(request => request.Checks is not null);
    }
}

internal sealed class PublishTaskValidationRequestValidator : AbstractValidator<PublishTaskValidationRequest>
{
    public PublishTaskValidationRequestValidator()
    {
        RuleFor(request => request.Version)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => Guid.TryParseExact(value, "D", out _))
            .WithMessage("Версия конфигурации должна быть UUID.");
    }
}

internal sealed class ValidationCheckRequestValidator : AbstractValidator<ValidationCheckRequest>
{
    public ValidationCheckRequestValidator()
    {
        RuleFor(check => check.Id)
            .NotEqual(Guid.Empty)
            .When(check => check.Id.HasValue);
        RuleFor(check => check.Kind).NotNull().IsInEnum();
        RuleFor(check => check.Weight).NotNull().InclusiveBetween(1, 100);
        RuleFor(check => check.Order).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(check => check.Value).MaximumLength(256);
    }
}

internal sealed class ValidationCheckPreviewRequestValidator : AbstractValidator<ValidationCheckPreviewRequest>
{
    public ValidationCheckPreviewRequestValidator()
    {
        RuleFor(check => check.Id)
            .NotEqual(Guid.Empty)
            .When(check => check.Id.HasValue);
        RuleFor(check => check.ClientKey)
            .NotEmpty()
            .MaximumLength(100)
            .When(check => !check.Id.HasValue);
        RuleFor(check => check.Kind).NotNull().IsInEnum();
        RuleFor(check => check.Weight).NotNull().InclusiveBetween(1, 100);
        RuleFor(check => check.Order).NotNull().GreaterThanOrEqualTo(0);
        RuleFor(check => check.Value).MaximumLength(256);
    }
}
