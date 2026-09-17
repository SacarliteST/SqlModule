using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Attempt;

/// <summary>Ответ с данными попытки выполнения задания.</summary>
public record AttemptResponse(
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
    AttemptResultSnapshotState ResultSnapshotState,
    string? TaskName = null,
    string? TopicName = null,
    string? PublicError = null,
    string? CreatedByName = null,
    string? UpdatedByName = null,
    IReadOnlyList<string>? ActualColumns = null,
    IReadOnlyList<IReadOnlyList<string?>>? ActualRows = null,
    int? ReturnedRowCount = null,
    bool IsResultTruncated = false,
    int? ResultRowLimit = null,
    DateTimeOffset? ResultSnapshotCreatedAt = null,
    DateTimeOffset? ResultSnapshotExpiresAt = null,
    global::SQLModule.Contracts.Training.Validation.AttemptScoringResponse? Scoring = null);
