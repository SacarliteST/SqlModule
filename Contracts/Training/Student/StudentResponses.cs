using SQLModule.Domain.Training;

namespace SQLModule.Contracts.Training.Student;

public record StudentTopicsRequest(int Offset = 0, int Limit = 100);

public record StudentTasksRequest(
    Guid? TopicId = null,
    string? Name = null,
    short? DifficultyLevel = null,
    int Offset = 0,
    int Limit = 20);

public record StudentAttemptsRequest(
    Guid? TaskId = null,
    Guid? TopicId = null,
    ExecutionStatus? Status = null,
    bool? IsCorrect = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null,
    int Offset = 0,
    int Limit = 20);

public record StudentTopicResponse(
    Guid Id,
    string TopicName,
    Guid? ParentTopicId,
    string? Description,
    int PublishedTasksCount);

public record StudentTaskResponse(
    Guid Id,
    Guid TopicId,
    string TopicName,
    string TaskName,
    string TaskTextPreview,
    short DifficultyLevel,
    string DbmsName);

public record StudentTaskDetailsResponse(
    Guid Id,
    Guid TopicId,
    string TopicName,
    string TaskName,
    string TaskText,
    short DifficultyLevel,
    string DbmsName,
    StudentExecutionLimitsResponse ExecutionLimits,
    global::SQLModule.Contracts.Training.Validation.StudentTaskValidationResponse? Validation = null);

public record StudentExecutionLimitsResponse(int TimeoutSeconds, int MaxRows, int MaxSqlLength);

public record StudentAttemptListItemResponse(
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
    int? AttemptNumber = null,
    int? Score = null,
    Guid? ProgressId = null,
    Guid? ValidationVersionId = null);

public record StudentAttemptResponse(
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
