using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Попытка студента выполнить SQL-задание.</summary>
public sealed class Attempt : AuditableEntity
{
    /// <summary>Идентификатор студента (мягкая ссылка на Identity-сервис).</summary>
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public string SubmittedSql { get; private set; } = String.Empty;
    public ExecutionStatus Status { get; private set; }
    public bool IsCorrect { get; private set; }
    public CheckReason Reason { get; private set; }
    public int? RowCount { get; private set; }
    public long? DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset FinishedAt { get; private set; }

    private Attempt(
        Guid id, Guid userId, Guid taskId, string submittedSql,
        ExecutionStatus status, bool isCorrect, CheckReason reason,
        int? rowCount, long? durationMs, string? errorMessage,
        DateTimeOffset startedAt, DateTimeOffset finishedAt) : base(id)
    {
        UserId = userId;
        TaskId = taskId;
        SubmittedSql = submittedSql;
        Status = status;
        IsCorrect = isCorrect;
        Reason = reason;
        RowCount = rowCount;
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    public static Attempt Record(
        Guid userId, Guid taskId, string submittedSql,
        ExecutionStatus status, bool isCorrect, CheckReason reason,
        int? rowCount, long? durationMs, string? errorMessage,
        DateTimeOffset startedAt, DateTimeOffset finishedAt,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), userId, taskId, submittedSql,
            status, isCorrect, reason, rowCount, durationMs, errorMessage,
            startedAt, finishedAt);
}
