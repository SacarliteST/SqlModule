using SQLModule.Domain.Common;

namespace SQLModule.Domain.ModuleIntegration;

/// <summary>Надёжно сохранённое сообщение для асинхронной доставки платформе.</summary>
public sealed class PendingPublish : BaseEntity
{
    public PendingPublishKind Kind { get; private set; }
    public Guid SessionId { get; private set; }
    public string DeduplicationKey { get; private set; }
    public string MessageJson { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? DeadLetterAt { get; private set; }

    private PendingPublish(
        Guid id,
        PendingPublishKind kind,
        Guid sessionId,
        string deduplicationKey,
        string messageJson,
        DateTimeOffset createdAt) : base(id)
    {
        Kind = kind;
        SessionId = sessionId;
        DeduplicationKey = deduplicationKey;
        MessageJson = messageJson;
        Attempts = 0;
        NextAttemptAt = createdAt;
        CreatedAt = createdAt;
    }

    public static PendingPublish Create(
        Guid id,
        PendingPublishKind kind,
        Guid sessionId,
        string deduplicationKey,
        string messageJson,
        DateTimeOffset createdAt) =>
        new(id, kind, sessionId, deduplicationKey, messageJson, createdAt);

    public void MarkSent(DateTimeOffset sentAt) => SentAt = sentAt;

    public void MarkDeadLetter(DateTimeOffset failedAt)
    {
        Attempts++;
        DeadLetterAt = failedAt;
    }

    public void RegisterFailure(
        DateTimeOffset failedAt,
        DateTimeOffset nextAttemptAt,
        bool moveToDeadLetter)
    {
        Attempts++;
        NextAttemptAt = nextAttemptAt;
        if (moveToDeadLetter)
        {
            DeadLetterAt = failedAt;
        }
    }
}
