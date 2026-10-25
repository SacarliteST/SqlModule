using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Факт выдачи подсказки студенту в открытом банке.</summary>
public sealed class BankHintAccess : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid AttemptId { get; private set; }
    public int AttemptNumber { get; private set; }
    public long AutoHintGroupsMask { get; private set; }
    public bool AuthorHintIssued { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }

    private BankHintAccess(
        Guid id,
        Guid userId,
        Guid taskId,
        Guid attemptId,
        int attemptNumber,
        long autoHintGroupsMask,
        bool authorHintIssued,
        DateTimeOffset requestedAt) : base(id)
    {
        UserId = userId;
        TaskId = taskId;
        AttemptId = attemptId;
        AttemptNumber = attemptNumber;
        AutoHintGroupsMask = autoHintGroupsMask;
        AuthorHintIssued = authorHintIssued;
        RequestedAt = requestedAt;
    }

    public static BankHintAccess Create(
        Guid userId,
        Guid taskId,
        Guid attemptId,
        int attemptNumber,
        IReadOnlyCollection<HintGroup> autoHintGroups,
        bool authorHintIssued,
        DateTimeOffset requestedAt,
        Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            userId,
            taskId,
            attemptId,
            attemptNumber,
            ToMask(autoHintGroups),
            authorHintIssued,
            requestedAt);

    public IReadOnlyList<HintGroup> GetAutoHintGroups() =>
        Enum.GetValues<HintGroup>()
            .Where(group => (AutoHintGroupsMask & (1L << (int)group)) != 0)
            .ToArray();

    private static long ToMask(IEnumerable<HintGroup> groups) =>
        groups.Aggregate(0L, (mask, group) => mask | (1L << (int)group));
}
