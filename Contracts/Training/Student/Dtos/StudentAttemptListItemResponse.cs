namespace SQLModule.Contracts.Training.Student;

/// <summary>Элемент личной истории попыток.</summary>
public sealed record StudentAttemptListItemResponse(
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
    Guid? ValidationVersionId = null);
