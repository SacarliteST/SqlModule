using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Web.Features.Training.Validation;

internal static class TaskValidationMappings
{
    internal static TaskValidationDefinition ToDefinition(TaskValidationConfiguration configuration) =>
        new(
            configuration.PassingScore,
            configuration.MaxAttempts,
            configuration.GetVisibleHintGroups(),
            configuration.Checks
                .OrderBy(check => check.Order)
                .Select(check => new ValidationCheckDefinition(
                    check.Id,
                    check.Kind,
                    check.Value,
                    check.Weight,
                    check.Order))
                .ToArray());

    internal static TaskValidationDefinition ToDefinition(TaskValidationConfigurationRequest request) =>
        new(
            request.PassingScore!.Value,
            request.MaxAttempts,
            request.VisibleHintGroups!,
            request.Checks!
                .Select(check => new ValidationCheckDefinition(
                    check.Id,
                    check.Kind!.Value,
                    check.Value,
                    check.Weight!.Value,
                    check.Order!.Value))
                .OrderBy(check => check.Order)
                .ToArray());

    internal static TaskValidationDefinition ToDefinition(TaskValidationPreviewRequest request) =>
        new(
            request.PassingScore!.Value,
            request.MaxAttempts,
            request.VisibleHintGroups!,
            request.Checks!
                .Select(check => new ValidationCheckDefinition(
                    check.Id,
                    check.Kind!.Value,
                    check.Value,
                    check.Weight!.Value,
                    check.Order!.Value))
                .OrderBy(check => check.Order)
                .ToArray());

    internal static IReadOnlyList<ValidationPreviewIdentity> ToIdentities(
        TaskValidationPreviewRequest request) =>
        request.Checks!
            .OrderBy(check => check.Order)
            .Select(check => new ValidationPreviewIdentity(check.Id, check.ClientKey))
            .ToArray();

    internal static IReadOnlyList<ValidationPreviewIdentity> ToIdentities(
        TaskValidationDefinition definition) =>
        definition.Checks
            .Select(check => new ValidationPreviewIdentity(check.Id, null))
            .ToArray();

    internal static TaskValidationConfigurationResponse ToResponse(
        TaskValidationConfiguration configuration,
        TaskValidationVersion? activeVersion,
        IReadOnlyDictionary<Guid, string> tableNames)
    {
        var hasUnpublishedChanges = activeVersion is null ||
                                    activeVersion.ConfigurationVersion != configuration.Version;
        return new TaskValidationConfigurationResponse(
            configuration.TaskId,
            configuration.Version.ToString("D"),
            activeVersion?.Id,
            activeVersion?.VersionNumber,
            hasUnpublishedChanges ? ValidationConfigurationState.Draft : ValidationConfigurationState.Published,
            hasUnpublishedChanges,
            configuration.PassingScore,
            configuration.MaxAttempts,
            configuration.GetVisibleHintGroups(),
            configuration.Checks
                .OrderBy(check => check.Order)
                .Select(check => new ValidationCheckResponse(
                    check.Id,
                    check.Kind,
                    check.Value,
                    GetValueDisplayName(check, tableNames),
                    check.Weight,
                    check.Order))
                .ToArray(),
            configuration.CreatedAt,
            configuration.UpdatedAt,
            activeVersion?.PublishedAt);
    }

    internal static Error ToError(TaskValidationPreviewResponse response)
    {
        var violation = response.Violations.FirstOrDefault();
        return violation is null
            ? TaskValidationErrors.ReferenceDoesNotScore100(
                $"Эталонное решение набирает {response.ReferenceScore} из 100 баллов.")
            : new Error(
                violation.Code,
                violation.Message,
                ErrorType.Validation,
                violation.Path,
                Severity: violation.Severity.ToString());
    }

    private static string? GetValueDisplayName(
        ValidationCheck check,
        IReadOnlyDictionary<Guid, string> tableNames)
    {
        if (check.Kind is ValidationCheckKind.RequiredTable or ValidationCheckKind.ForbiddenTable &&
            Guid.TryParse(check.Value, out var tableId) &&
            tableNames.TryGetValue(tableId, out var tableName))
        {
            return tableName;
        }

        return check.Value;
    }
}
