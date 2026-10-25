using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Факт открытия разбора решения студентом.</summary>
public sealed class BankSolutionAccess : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }

    private BankSolutionAccess(Guid id, Guid userId, Guid taskId, DateTimeOffset openedAt) : base(id)
    {
        UserId = userId;
        TaskId = taskId;
        OpenedAt = openedAt;
    }

    public static BankSolutionAccess Create(
        Guid userId,
        Guid taskId,
        DateTimeOffset openedAt,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), userId, taskId, openedAt);
}
