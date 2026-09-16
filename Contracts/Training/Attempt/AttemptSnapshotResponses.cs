using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Сохранённый безопасный фактический результат попытки.</summary>
public record AttemptResultSnapshotResponse(
    AttemptResultSnapshotState ResultSnapshotState,
    IReadOnlyList<string>? ActualColumns,
    IReadOnlyList<IReadOnlyList<string?>>? ActualRows,
    int? ReturnedRowCount,
    bool IsResultTruncated,
    int? ResultRowLimit,
    DateTimeOffset? ResultSnapshotCreatedAt,
    DateTimeOffset? ResultSnapshotExpiresAt);

/// <summary>Элемент списка журнала попыток без тяжёлого snapshot.</summary>
public record AttemptListItemResponse(
    Guid Id,
    Guid UserId,
    string StudentName,
    Guid TaskId,
    string SubmittedSql,
    ExecutionStatus Status,
    bool IsCorrect,
    CheckReason Reason,
    int? RowCount,
    long? DurationMs,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt,
    string? TaskName = null,
    string? TopicName = null,
    string? PublicError = null,
    string? CreatedByName = null,
    string? UpdatedByName = null,
    Guid? ProgressId = null,
    Guid? ValidationVersionId = null,
    int? AttemptNumber = null,
    int? Score = null);
