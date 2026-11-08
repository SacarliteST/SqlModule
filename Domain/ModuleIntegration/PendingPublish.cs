using SQLModule.Domain.Common;

namespace SQLModule.Domain.ModuleIntegration;

/// <summary>Надёжно сохранённое сообщение для асинхронной доставки платформе.</summary>
public sealed class PendingPublish : BaseEntity
{
    public PendingPublishKind Kind { get; private set; }
    public Guid SessionId { get; private set; }
    public string DeduplicationKey { get; private set; }
    public string MessageJson { get; private set; }
    public string? Topic { get; private set; }
    public string? MessageKey { get; private set; }
    public string? TraceParent { get; private set; }
    public string? TraceState { get; private set; }
    public string? LastErrorCode { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? DeadLetterAt { get; private set; }

    /// <summary>Владелец текущей аренды сообщения (экземпляр процессора), пока оно обрабатывается.</summary>
    public string? LockedBy { get; private set; }

    /// <summary>Момент истечения аренды; после него сообщение может забрать другой экземпляр.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    private PendingPublish(
        Guid id,
        PendingPublishKind kind,
        Guid sessionId,
        string deduplicationKey,
        string messageJson,
        string? topic,
        string? messageKey,
        string? traceParent,
        string? traceState,
        DateTimeOffset createdAt) : base(id)
    {
        Kind = kind;
        SessionId = sessionId;
        DeduplicationKey = deduplicationKey;
        MessageJson = messageJson;
        Topic = topic;
        MessageKey = messageKey;
        TraceParent = traceParent;
        TraceState = traceState;
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
        new(id, kind, sessionId, deduplicationKey, messageJson, null, null, null, null, createdAt);

    public static PendingPublish CreateKafka(
        Guid id,
        PendingPublishKind kind,
        Guid aggregateId,
        string deduplicationKey,
        string topic,
        string messageKey,
        string messageJson,
        DateTimeOffset createdAt,
        string? traceParent = null,
        string? traceState = null) =>
        new(id, kind, aggregateId, deduplicationKey, messageJson, topic, messageKey, traceParent, traceState, createdAt);

    public void MarkSent(DateTimeOffset sentAt)
    {
        SentAt = sentAt;
        ReleaseLock();
    }

    /// <summary>Снимает аренду: сообщение снова доступно для claim (по NextAttemptAt).</summary>
    public void ReleaseLock()
    {
        LockedBy = null;
        LockedUntil = null;
    }

    public void MarkDeadLetter(DateTimeOffset failedAt)
    {
        Attempts++;
        DeadLetterAt = failedAt;
        ReleaseLock();
    }

    public void RegisterFailure(
        DateTimeOffset failedAt,
        DateTimeOffset nextAttemptAt,
        bool moveToDeadLetter,
        string errorCode = "Unknown")
    {
        Attempts++;
        NextAttemptAt = nextAttemptAt;
        LastErrorCode = errorCode;
        ReleaseLock();
        if (moveToDeadLetter)
        {
            DeadLetterAt = failedAt;
        }
    }
}
