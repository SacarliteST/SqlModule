using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Одна непрерывная работа студента над неизменяемой версией SQL-задания.</summary>
public sealed class LearningEpisode : BaseEntity
{
    private readonly List<LearningEpisodeSkillResult> skillResults = [];

    public Guid StudentId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid ValidationVersionId { get; private set; }
    public Guid? ModuleSessionId { get; private set; }
    public LearningEpisodeStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset LastHeartbeatAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public LearningEpisodeFinalizationReason? FinalizationReason { get; private set; }
    public Guid? FinalAttemptId { get; private set; }
    public bool ProducesObservation { get; private set; }

    public IReadOnlyCollection<LearningEpisodeSkillResult> SkillResults => skillResults.AsReadOnly();

    private LearningEpisode(
        Guid id,
        Guid studentId,
        Guid taskId,
        Guid validationVersionId,
        Guid? moduleSessionId,
        DateTimeOffset startedAt) : base(id)
    {
        StudentId = studentId;
        TaskId = taskId;
        ValidationVersionId = validationVersionId;
        ModuleSessionId = moduleSessionId;
        Status = LearningEpisodeStatus.Active;
        StartedAt = startedAt;
        LastHeartbeatAt = startedAt;
    }

    public static LearningEpisode Start(
        Guid studentId,
        Guid taskId,
        Guid validationVersionId,
        Guid? moduleSessionId,
        DateTimeOffset startedAt) =>
        new(Guid.NewGuid(), studentId, taskId, validationVersionId, moduleSessionId, startedAt);

    public void Heartbeat(DateTimeOffset occurredAt)
    {
        if (Status != LearningEpisodeStatus.Active)
        {
            throw new InvalidOperationException("Финализированный учебный эпизод нельзя возобновить.");
        }

        if (occurredAt > LastHeartbeatAt)
        {
            LastHeartbeatAt = occurredAt;
        }
    }

    public void Finalize(
        LearningEpisodeFinalizationReason reason,
        DateTimeOffset finalizedAt,
        Guid? finalAttemptId,
        IReadOnlyCollection<LearningEpisodeSkillResult> results)
    {
        if (Status == LearningEpisodeStatus.Finalized)
        {
            return;
        }

        if (results.GroupBy(result => result.SkillId).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Один навык не может повторяться в результате эпизода.", nameof(results));
        }

        Status = LearningEpisodeStatus.Finalized;
        FinalizationReason = reason;
        FinalizedAt = finalizedAt;
        FinalAttemptId = finalAttemptId;
        ProducesObservation = results.Count > 0;
        skillResults.AddRange(results);
    }
}
