namespace SQLModule.Contracts.Training.Student;

/// <summary>Безопасные подробности личной попытки.</summary>
public sealed record StudentAttemptResponse(
    Guid Id,
    Guid TaskId,
    string TaskName,
    Guid TopicId,
    string TopicName,
    string SubmittedSql,
    ExecutionStatus Status,
    bool IsCorrect,
    CheckReason Reason,
    int? RowCount,
    long? DurationMs,
    string? PublicError,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    AttemptResultSnapshotState ResultSnapshotState,
    IReadOnlyList<string>? ActualColumns,
    IReadOnlyList<IReadOnlyList<string?>>? ActualRows,
    int? ReturnedRowCount,
    bool IsResultTruncated,
    int? ResultRowLimit,
    DateTimeOffset? ResultSnapshotCreatedAt,
    DateTimeOffset? ResultSnapshotExpiresAt,
    global::SQLModule.Contracts.Training.Validation.AttemptScoringResponse? Scoring = null);
