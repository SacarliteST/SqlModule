using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

public enum LearningEpisodeSkillSource
{
    ValidationRule,
    TaskTag
}

public enum LearningEpisodeSkillOutcome
{
    Failed,
    Succeeded
}

/// <summary>Окончательный бинарный результат одного навыка в учебном эпизоде.</summary>
public sealed class LearningEpisodeSkillResult : BaseEntity
{
    public Guid EpisodeId { get; private set; }
    public Guid SkillId { get; private set; }
    public string SkillExternalId { get; private set; }
    public LearningEpisodeSkillSource Source { get; private set; }
    public LearningEpisodeSkillOutcome Outcome { get; private set; }

    public LearningEpisode Episode { get; private set; } = null!;
    public Skill Skill { get; private set; } = null!;

    private LearningEpisodeSkillResult(
        Guid id,
        Guid episodeId,
        Guid skillId,
        string skillExternalId,
        LearningEpisodeSkillSource source,
        LearningEpisodeSkillOutcome outcome) : base(id)
    {
        EpisodeId = episodeId;
        SkillId = skillId;
        SkillExternalId = skillExternalId;
        Source = source;
        Outcome = outcome;
    }

    public static LearningEpisodeSkillResult Create(
        Guid episodeId,
        Guid skillId,
        string skillExternalId,
        LearningEpisodeSkillSource source,
        LearningEpisodeSkillOutcome outcome) =>
        new(Guid.NewGuid(), episodeId, skillId, skillExternalId, source, outcome);
}
