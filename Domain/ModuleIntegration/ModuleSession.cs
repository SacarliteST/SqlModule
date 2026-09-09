using SQLModule.Domain.Common;

namespace SQLModule.Domain.ModuleIntegration;

/// <summary>Доверенный контекст платформенного запуска, полученный от Education.</summary>
public sealed class ModuleSession : AuditableEntity
{
    public string SessionKey { get; private set; }
    public Guid UserId { get; private set; }
    public string TaskRef { get; private set; }
    public string ReturnUrl { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public ModuleSessionStatus Status { get; private set; }

    private ModuleSession(
        Guid id,
        string sessionKey,
        Guid userId,
        string taskRef,
        string returnUrl,
        DateTimeOffset? expiresAt) : base(id)
    {
        SessionKey = sessionKey;
        UserId = userId;
        TaskRef = taskRef;
        ReturnUrl = returnUrl;
        ExpiresAt = expiresAt;
        Status = ModuleSessionStatus.Active;
    }

    public static ModuleSession Create(
        Guid sessionId,
        string sessionKey,
        Guid userId,
        string taskRef,
        string returnUrl,
        DateTimeOffset? expiresAt = null)
        => new(sessionId, sessionKey, userId, taskRef, returnUrl, expiresAt);

    public bool IsExpired(DateTimeOffset now) => ExpiresAt.HasValue && ExpiresAt.Value < now;

    /// <summary>Обновляет доверенный снимок активной сессии повторным push от Education.</summary>
    public void Synchronize(
        string sessionKey,
        Guid userId,
        string taskRef,
        string returnUrl,
        DateTimeOffset? expiresAt)
    {
        if (Status != ModuleSessionStatus.Active)
        {
            throw new InvalidOperationException("Завершённую платформенную сессию нельзя обновить.");
        }

        SessionKey = sessionKey;
        UserId = userId;
        TaskRef = taskRef;
        ReturnUrl = returnUrl;
        ExpiresAt = expiresAt;
    }

    public void MarkCompletionPending()
    {
        if (Status != ModuleSessionStatus.Active)
        {
            throw new InvalidOperationException("Только активная сессия может ожидать завершения.");
        }

        Status = ModuleSessionStatus.CompletionPending;
    }

    public void MarkCompleted()
    {
        if (Status == ModuleSessionStatus.Completed)
        {
            return;
        }

        if (Status is not (ModuleSessionStatus.Active or ModuleSessionStatus.CompletionPending))
        {
            throw new InvalidOperationException("Сессию в текущем состоянии нельзя завершить.");
        }

        Status = ModuleSessionStatus.Completed;
    }

    public void MarkCompletionFailed()
    {
        if (Status != ModuleSessionStatus.CompletionPending)
        {
            throw new InvalidOperationException("Ошибка завершения допустима только для ожидающей сессии.");
        }

        Status = ModuleSessionStatus.CompletionFailed;
    }

    public bool MarkExpired(DateTimeOffset now)
    {
        if (Status != ModuleSessionStatus.Active || !IsExpired(now))
        {
            return false;
        }

        Status = ModuleSessionStatus.Expired;
        return true;
    }
}
