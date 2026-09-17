using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Результат проверки попытки выполнения задания.</summary>
public record SubmitAttemptResponse(
    Guid AttemptId,
    ExecutionStatus Status,
    bool IsCorrect,
    CheckReason Reason,
    int? RowCount,
    long? DurationMs,
    string? PublicError,
    IReadOnlyList<string>? ActualColumns,
    IReadOnlyList<IReadOnlyList<string?>>? ActualRows,
    bool IsResultTruncated,
    AttemptResultSnapshotState ResultSnapshotState,
    int? ReturnedRowCount = null,
    int? ResultRowLimit = null,
    DateTimeOffset? ResultSnapshotCreatedAt = null,
    DateTimeOffset? ResultSnapshotExpiresAt = null,
    Guid? ProgressId = null,
    Guid? ValidationVersionId = null,
    int? AttemptNumber = null,
    int? Score = null,
    int? BestScore = null,
    int? PassingScore = null,
    bool? IsPassed = null,
    int? AttemptsUsed = null,
    int? AttemptsRemaining = null,
    bool? CanSubmit = null,
    bool? CanFinalize = null,
    IReadOnlyList<global::SQLModule.Contracts.Training.Validation.AttemptCheckResultResponse>? Checks = null,
    IReadOnlyList<global::SQLModule.Contracts.Training.Validation.AttemptHintResponse>? Hints = null);
