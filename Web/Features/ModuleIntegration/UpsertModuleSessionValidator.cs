using SQLModule.Application.ModuleIntegration;
﻿using FluentValidation;
using SQLModule.Contracts.ModuleIntegration;
using Microsoft.Extensions.Options;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed class UpsertModuleSessionValidator : AbstractValidator<UpsertModuleSessionRequest>
{
    public UpsertModuleSessionValidator(IOptions<ModuleIntegrationOptions> options)
    {
        RuleFor(request => request.SessionId).NotNull().NotEmpty();
        RuleFor(request => request.SessionKey).NotEmpty().MaximumLength(512);
        RuleFor(request => request.UserId).NotNull().NotEmpty();
        RuleFor(request => request.TaskRef)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(128)
            .Must(taskRef => Guid.TryParse(taskRef, out _))
            .WithMessage("TaskRef должен содержать идентификатор SQL-задания.");
        RuleFor(request => request.ReturnUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(IsAbsoluteHttpUrl)
            .WithMessage("ReturnUrl должен быть абсолютным HTTP(S)-адресом.");
        RuleFor(request => request.MaxAttempts)
            .InclusiveBetween(1, options.Value.MaxAttemptsPerSession)
            .When(request => request.MaxAttempts.HasValue);
    }

    private static bool IsAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
