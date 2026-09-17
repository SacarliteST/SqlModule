using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Contracts.Training.Validation;

/// <summary>Полная редактируемая конфигурация проверки задания.</summary>
public sealed record TaskValidationConfigurationRequest(
    string? Version,
    int? PassingScore,
    int? MaxAttempts,
    IReadOnlyList<HintGroup>? VisibleHintGroups,
    IReadOnlyList<ValidationCheckRequest>? Checks);

/// <summary>Критерий в запросе сохранения конфигурации.</summary>
public sealed record ValidationCheckRequest(
    Guid? Id,
    ValidationCheckKind? Kind,
    string? Value,
    int? Weight,
    int? Order);

/// <summary>Критерий для preview с временным идентификатором frontend.</summary>
public sealed record ValidationCheckPreviewRequest(
    Guid? Id,
    string? ClientKey,
    ValidationCheckKind? Kind,
    string? Value,
    int? Weight,
    int? Order);

/// <summary>Сохранённый критерий проверки.</summary>
public sealed record ValidationCheckResponse(
    Guid Id,
    ValidationCheckKind Kind,
    string? Value,
    string? ValueDisplayName,
    int Weight,
    int Order);

/// <summary>Текущая конфигурация проверки и активная опубликованная версия.</summary>
public sealed record TaskValidationConfigurationResponse(
    Guid TaskId,
    string Version,
    Guid? ValidationVersionId,
    int? ValidationVersionNumber,
    ValidationConfigurationState State,
    bool HasUnpublishedChanges,
    int PassingScore,
    int? MaxAttempts,
    IReadOnlyList<HintGroup> VisibleHintGroups,
    IReadOnlyList<ValidationCheckResponse> Checks,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

/// <summary>Draft-конфигурация для проверки эталона без публикации версии.</summary>
public sealed record TaskValidationPreviewRequest(
    int? PassingScore,
    int? MaxAttempts,
    IReadOnlyList<HintGroup>? VisibleHintGroups,
    IReadOnlyList<ValidationCheckPreviewRequest>? Checks);

/// <summary>Результат одного критерия при preview.</summary>
public sealed record ValidationCheckPreviewResponse(
    Guid? CheckId,
    string? ClientKey,
    ValidationCheckKind Kind,
    ValidationCheckStatus Status,
    int AwardedScore,
    string? Message);

/// <summary>Безопасное нарушение конфигурации или эталонного решения.</summary>
public sealed record ValidationViolationResponse(
    string Path,
    string Code,
    ValidationViolationSeverity Severity,
    string Message);

/// <summary>Предварительная проверка эталона по draft-конфигурации.</summary>
public sealed record TaskValidationPreviewResponse(
    bool IsValid,
    int ReferenceScore,
    string AnalyzerVersion,
    IReadOnlyList<ValidationCheckPreviewResponse> Checks,
    IReadOnlyList<ValidationViolationResponse> Violations);

/// <summary>Запрос публикации проверенной immutable validation version.</summary>
public sealed record PublishTaskValidationRequest(string? Version);
