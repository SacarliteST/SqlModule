using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Одно standalone- или platform-прохождение SQL-задания.</summary>
public sealed class StudentTaskProgress : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid? ModuleSessionId { get; private set; }
    public Guid ValidationVersionId { get; private set; }
    public int AttemptsUsed { get; private set; }
    public int NextAttemptNumber { get; private set; }
    public int BestScore { get; private set; }
    public ProgressStatus Status { get; private set; }
    public int? FinalScore { get; private set; }
    public FinalizationReason? FinalizationReason { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public Guid ConcurrencyVersion { get; private set; }

    public SqlTask Task { get; private set; } = null!;
    public TaskValidationVersion ValidationVersion { get; private set; } = null!;

    private StudentTaskProgress(
        Guid id,
        Guid userId,
        Guid taskId,
        Guid validationVersionId,
        Guid? moduleSessionId,
        DateTimeOffset? expiresAt) : base(id)
    {
        UserId = userId;
        TaskId = taskId;
        ValidationVersionId = validationVersionId;
        ModuleSessionId = moduleSessionId;
        ExpiresAt = expiresAt;
        AttemptsUsed = 0;
        NextAttemptNumber = 1;
        BestScore = 0;
        Status = ProgressStatus.Active;
        ConcurrencyVersion = Guid.NewGuid();
    }

    public static StudentTaskProgress CreateStandalone(
        Guid userId,
        Guid taskId,
        Guid validationVersionId,
        DateTimeOffset? expiresAt = null,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), userId, taskId, validationVersionId, null, expiresAt);

    public static StudentTaskProgress CreatePlatform(
        Guid userId,
        Guid taskId,
        Guid validationVersionId,
        Guid moduleSessionId,
        DateTimeOffset? expiresAt,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), userId, taskId, validationVersionId, moduleSessionId, expiresAt);

    public int ReserveAttemptNumber()
    {
        var reserved = NextAttemptNumber;
        NextAttemptNumber++;
        Touch();
        return reserved;
    }

    public void RecordCountedAttempt(int score)
    {
        AttemptsUsed++;
        BestScore = Math.Max(BestScore, score);
        Touch();
    }

    public void BeginFinalization(FinalizationReason reason, DateTimeOffset finalizedAt)
    {
        FinalScore = BestScore;
        FinalizationReason = reason;
        FinalizedAt = finalizedAt;
        Status = ProgressStatus.Finalizing;
        Touch();
    }

    public void MarkCompletionPending()
    {
        Status = ProgressStatus.CompletionPending;
        Touch();
    }

    public void MarkCompletionFailed()
    {
        Status = ProgressStatus.CompletionFailed;
        Touch();
    }

    public void MarkCompleted()
    {
        Status = ProgressStatus.Completed;
        Touch();
    }

    public void CloseForRestart(DateTimeOffset finalizedAt)
    {
        FinalScore = BestScore;
        FinalizationReason = global::SQLModule.Domain.Training.FinalizationReason.Restarted;
        FinalizedAt = finalizedAt;
        Status = ProgressStatus.Completed;
        Touch();
    }

    public void SynchronizePlatformExpiry(DateTimeOffset? expiresAt)
    {
        if (!ModuleSessionId.HasValue)
        {
            throw new InvalidOperationException("Срок платформенной сессии неприменим к standalone-прохождению.");
        }

        ExpiresAt = expiresAt;
        Touch();
    }

    public void MarkExpired(DateTimeOffset finalizedAt)
    {
        FinalScore = BestScore;
        FinalizationReason = global::SQLModule.Domain.Training.FinalizationReason.Expired;
        FinalizedAt = finalizedAt;
        Status = ProgressStatus.Expired;
        Touch();
    }

    private void Touch() => ConcurrencyVersion = Guid.NewGuid();
}
