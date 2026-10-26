
using System.Text.Json.Serialization;

namespace SQLModule.Contracts.Training.Validation;

/// <summary>Таблица, раскрытая студенту как безопасная подсказка.</summary>
public sealed record StudentHintTableResponse(Guid Id, string Name);

/// <summary>Разрешённые преподавателем подсказки по validation version.</summary>
public sealed record StudentTaskHintsResponse(
    IReadOnlyList<HintGroup> Groups,
    IReadOnlyList<SqlConstruct> RequiredConstructs,
    IReadOnlyList<SqlConstruct> ForbiddenConstructs,
    IReadOnlyList<StudentHintTableResponse> RequiredTables,
    IReadOnlyList<StudentHintTableResponse> ForbiddenTables);

/// <summary>Безопасная validation-часть student task details.</summary>
public sealed record StudentTaskValidationResponse(
    int PassingScore,
    StudentSessionStateResponse? Session,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    StudentTaskHintsResponse? Hints);

/// <summary>Состояние платформенной сессии; отсутствует в standalone-режиме.</summary>
public sealed record StudentSessionStateResponse(
    SessionStatus Status,
    int BestScore,
    int AttemptsUsed,
    int? MaxAttempts,
    int? AttemptsRemaining,
    bool CanSubmit,
    bool CanFinalize,
    bool IsPassed,
    DateTimeOffset? ExpiresAt,
    int? FinalScore,
    FinalizationReason? FinalizationReason,
    DateTimeOffset? FinalizedAt);

/// <summary>Результат ручного завершения платформенной сессии.</summary>
public sealed record SessionFinalizationResponse(
    SessionStatus Status,
    int BestScore,
    int FinalScore,
    bool IsPassed,
    FinalizationReason Reason,
    DateTimeOffset FinalizedAt,
    bool CanReturnToEducation,
    string? ReturnUrl);
