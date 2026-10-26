namespace SQLModule.Contracts.Training.SqlTask;

/// <summary>Краткая попытка студента.</summary>
public sealed record TeacherTaskAttemptResponse(
    Guid AttemptId,
    Guid StudentId,
    string StudentName,
    bool IsCorrect,
    ExecutionStatus Status,
    long? DurationMs,
    DateTimeOffset FinishedAt,
    int? AttemptNumber = null,
    int? Score = null);
