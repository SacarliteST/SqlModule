using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Короткая персистентная резервация номера попытки до AST/sandbox выполнения.</summary>
public sealed class AttemptReservation : BaseEntity
{
    public Guid ProgressId { get; private set; }
    public int AttemptNumber { get; private set; }
    public Guid IdempotencyKey { get; private set; }
    public string PayloadHash { get; private set; }
    public AttemptReservationState State { get; private set; }
    public Guid? AttemptId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public StudentTaskProgress Progress { get; private set; } = null!;

    private AttemptReservation(
        Guid id,
        Guid progressId,
        int attemptNumber,
        Guid idempotencyKey,
        string payloadHash,
        DateTimeOffset createdAt) : base(id)
    {
        ProgressId = progressId;
        AttemptNumber = attemptNumber;
        IdempotencyKey = idempotencyKey;
        PayloadHash = payloadHash;
        State = AttemptReservationState.Reserved;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static AttemptReservation Create(
        Guid progressId,
        int attemptNumber,
        Guid idempotencyKey,
        string payloadHash,
        DateTimeOffset createdAt,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), progressId, attemptNumber, idempotencyKey, payloadHash, createdAt);

    public void Complete(Guid attemptId, DateTimeOffset completedAt)
    {
        AttemptId = attemptId;
        State = AttemptReservationState.Completed;
        UpdatedAt = completedAt;
    }

    public void Release(DateTimeOffset releasedAt)
    {
        State = AttemptReservationState.Released;
        UpdatedAt = releasedAt;
    }

    public void Reopen(int attemptNumber, DateTimeOffset reopenedAt)
    {
        AttemptNumber = attemptNumber;
        AttemptId = null;
        State = AttemptReservationState.Reserved;
        UpdatedAt = reopenedAt;
    }
}
