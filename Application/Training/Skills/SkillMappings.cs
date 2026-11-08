using SQLModule.Contracts.Training.Skill;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Skills;

public static class SkillMappings
{
    public const string SemanticChangeWarning =
        "Изменение названия или описания не меняет идентичность навыка. Преподаватель отвечает за сохранение его педагогического смысла.";

    public static SkillResponse ToResponse(Skill skill) => new(
        skill.Id,
        skill.SkillId,
        skill.Name,
        skill.Description,
        (SkillOriginResponse)skill.Origin,
        skill.IsDeleted,
        skill.CreatedAt,
        skill.UpdatedAt,
        SemanticChangeWarning);
}
