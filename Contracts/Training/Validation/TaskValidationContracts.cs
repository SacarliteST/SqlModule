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
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="Version">Токен версии черновика; передаётся при сохранении и публикации.</param>
/// <param name="ValidationVersionId">
/// Идентификатор активной опубликованной версии. <c>null</c> — версия ни разу не публиковалась
/// (задание нельзя опубликовать или запустить). Отсутствие версии определяется только этим полем.
/// </param>
/// <param name="ValidationVersionNumber">Номер активной версии; <c>null</c>, если версия не публиковалась.</param>
/// <param name="State">Состояние: <c>Draft</c> — версии нет или черновик изменён, <c>Published</c> — совпадает с активной.</param>
/// <param name="HasUnpublishedChanges">
/// Черновик отличается от активной опубликованной версии. Если версии нет (<c>validationVersionId == null</c>),
/// значение <c>false</c>: сравнивать не с чем. Поле не означает отсутствие версии.
/// </param>
/// <param name="PassingScore">Проходной балл (1–100).</param>
/// <param name="MaxAttempts">Лимит попыток; <c>null</c> — без ограничения.</param>
/// <param name="VisibleHintGroups">Группы подсказок, видимые студенту.</param>
/// <param name="Checks">Критерии оценки.</param>
/// <param name="CreatedAt">Дата создания конфигурации.</param>
/// <param name="UpdatedAt">Дата последнего изменения черновика.</param>
/// <param name="PublishedAt">Дата публикации активной версии; <c>null</c>, если версии нет.</param>
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
