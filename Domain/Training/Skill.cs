using SQLModule.Domain.Common;

namespace SQLModule.Domain.Training;

/// <summary>Переиспользуемый навык SQL-модуля.</summary>
public sealed class Skill : AuditableEntity
{
    public string SkillId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public SkillOrigin Origin { get; private set; }
    public bool IsDeleted { get; private set; }

    private Skill(
        Guid id,
        string skillId,
        string name,
        string? description,
        SkillOrigin origin) : base(id)
    {
        SkillId = skillId;
        Name = name;
        Description = description;
        Origin = origin;
    }

    public static Skill CreateTeacherDefined(
        string skillId,
        string name,
        string? description = null,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), skillId, name, description, SkillOrigin.TeacherDefined);

    public static Skill CreateSystem(
        string skillId,
        string name,
        string? description = null,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), skillId, name, description, SkillOrigin.System);

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void Delete() => IsDeleted = true;

    public void Restore() => IsDeleted = false;
}
