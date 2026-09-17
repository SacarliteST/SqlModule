using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Validation;

/// <summary>Текущее прохождение SQL-задания.</summary>
public sealed record StudentTaskProgressResponse(
    Guid Id,
    Guid TaskId,
    ProgressStatus Status,
    Guid ValidationVersionId,
    int BestScore,
    int AttemptsUsed,
    int? AttemptsRemaining,
    bool CanSubmit,
    bool CanFinalize,
    bool IsPassed,
    DateTimeOffset? ExpiresAt,
    int? FinalScore,
    FinalizationReason? FinalizationReason,
    DateTimeOffset? FinalizedAt);

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
    int? MaxAttempts,
    StudentTaskProgressResponse? Progress,
    StudentTaskHintsResponse Hints);

/// <summary>Результат завершения standalone- или platform-прохождения.</summary>
public sealed record ProgressFinalizationResponse(
    Guid ProgressId,
    ProgressStatus Status,
    int BestScore,
    int FinalScore,
    bool IsPassed,
    FinalizationReason Reason,
    DateTimeOffset FinalizedAt,
    bool CanReturnToEducation,
    string? ReturnUrl);
