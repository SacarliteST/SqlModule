using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Навык-тег редактируемой конфигурации задания.</summary>
public sealed class TaskValidationSkillTag : BaseEntity
{
    public Guid ConfigurationId { get; private set; }
    public Guid SkillId { get; private set; }

    public TaskValidationConfiguration Configuration { get; private set; } = null!;
    public Skill Skill { get; private set; } = null!;

    private TaskValidationSkillTag(Guid id, Guid configurationId, Guid skillId) : base(id)
    {
        ConfigurationId = configurationId;
        SkillId = skillId;
    }

    public static TaskValidationSkillTag Create(
        Guid configurationId,
        Guid skillId,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), configurationId, skillId);
}
