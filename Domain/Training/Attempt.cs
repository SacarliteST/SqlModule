using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Попытка пользователя выполнить SQL-задание.</summary>
public sealed class Attempt : AuditableEntity
{
    /// <summary>Идентификатор пользователя (мягкая ссылка на Identity-сервис).</summary>
    public Guid UserId { get; private set; }
    public bool IsSuccess { get; private set; }
    public DateTimeOffset StartAttempt { get; private set; }
    public DateTimeOffset EndAttempt { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid QueryId { get; private set; }

    private Attempt(Guid id, Guid userId, bool isSuccess,
        DateTimeOffset startAttempt, DateTimeOffset endAttempt,
        Guid taskId, Guid queryId) : base(id)
    {
        UserId = userId;
        IsSuccess = isSuccess;
        StartAttempt = startAttempt;
        EndAttempt = endAttempt;
        TaskId = taskId;
        QueryId = queryId;
    }

    public static Attempt Create(
        Guid userId, bool isSuccess,
        DateTimeOffset startAttempt, DateTimeOffset endAttempt,
        Guid taskId, Guid queryId,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(), userId, isSuccess, startAttempt, endAttempt, taskId, queryId);

    public void Update(bool isSuccess, DateTimeOffset endAttempt)
    {
        IsSuccess = isSuccess;
        EndAttempt = endAttempt;
    }
}
