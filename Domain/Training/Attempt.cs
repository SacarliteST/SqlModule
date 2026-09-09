using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Попытка студента выполнить SQL-задание.</summary>
public sealed class Attempt : AuditableEntity
{
    /// <summary>Идентификатор студента (мягкая ссылка на Identity-сервис).</summary>
    public Guid UserId { get; private set; }
    /// <summary>Отображаемое имя студента на момент отправки попытки.</summary>
    public string StudentName { get; private set; }
    /// <summary>Email студента на момент отправки попытки.</summary>
    public string? StudentEmail { get; private set; }
    public Guid TaskId { get; private set; }
    /// <summary>Платформенная сессия запуска; null для standalone-попытки.</summary>
    public Guid? ModuleSessionId { get; private set; }
    public string SubmittedSql { get; private set; } = String.Empty;
    public ExecutionStatus Status { get; private set; }
    public bool IsCorrect { get; private set; }
    public CheckReason Reason { get; private set; }
    public int? RowCount { get; private set; }
    public long? DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset FinishedAt { get; private set; }
    public AttemptResultSnapshotState ResultSnapshotState { get; private set; }
    public string? ActualColumnsJson { get; private set; }
    public string? ActualRowsJson { get; private set; }
    public int? ReturnedRowCount { get; private set; }
    public bool IsResultTruncated { get; private set; }
    public int? ResultRowLimit { get; private set; }
    public DateTimeOffset? ResultSnapshotCreatedAt { get; private set; }
    public DateTimeOffset? ResultSnapshotExpiresAt { get; private set; }

    private Attempt(
        Guid id, Guid userId, string studentName, string? studentEmail, Guid taskId, string submittedSql,
        ExecutionStatus status, bool isCorrect, CheckReason reason,
        int? rowCount, long? durationMs, string? errorMessage,
        DateTimeOffset startedAt, DateTimeOffset finishedAt, Guid? moduleSessionId) : base(id)
    {
        UserId = userId;
        StudentName = studentName;
        StudentEmail = NormalizeStudentEmail(studentEmail);
        TaskId = taskId;
        ModuleSessionId = moduleSessionId;
        SubmittedSql = submittedSql;
        Status = status;
        IsCorrect = isCorrect;
        Reason = reason;
        RowCount = rowCount;
        DurationMs = durationMs;
        ErrorMessage = errorMessage;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        ResultSnapshotState = AttemptResultSnapshotState.NotStored;
    }

    public static Attempt Record(
        Guid userId, Guid taskId, string submittedSql,
        ExecutionStatus status, bool isCorrect, CheckReason reason,
        int? rowCount, long? durationMs, string? errorMessage,
        DateTimeOffset startedAt, DateTimeOffset finishedAt,
        Guid? id = null,
        string? studentName = null,
        string? studentEmail = null,
        Guid? moduleSessionId = null)
        => new(id ?? Guid.NewGuid(), userId, NormalizeStudentName(studentName, userId), studentEmail, taskId, submittedSql,
            status, isCorrect, reason, rowCount, durationMs, errorMessage,
            startedAt, finishedAt, moduleSessionId);

    private static string NormalizeStudentName(string? studentName, Guid userId)
        => String.IsNullOrWhiteSpace(studentName) ? userId.ToString() : studentName.Trim();

    private static string? NormalizeStudentEmail(string? studentEmail)
        => String.IsNullOrWhiteSpace(studentEmail) ? null : studentEmail.Trim();

    public void StoreResultSnapshot(
        string columnsJson,
        string rowsJson,
        int returnedRowCount,
        bool isTruncated,
        int rowLimit,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        ResultSnapshotState = AttemptResultSnapshotState.Available;
        ActualColumnsJson = columnsJson;
        ActualRowsJson = rowsJson;
        ReturnedRowCount = returnedRowCount;
        IsResultTruncated = isTruncated;
        ResultRowLimit = rowLimit;
        ResultSnapshotCreatedAt = createdAt;
        ResultSnapshotExpiresAt = expiresAt;
    }

    public void MarkResultNotProduced(int rowLimit, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        ResultSnapshotState = AttemptResultSnapshotState.NotProduced;
        ActualColumnsJson = null;
        ActualRowsJson = null;
        ReturnedRowCount = null;
        IsResultTruncated = false;
        ResultRowLimit = rowLimit;
        ResultSnapshotCreatedAt = createdAt;
        ResultSnapshotExpiresAt = expiresAt;
    }
}
