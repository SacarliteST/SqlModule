namespace SQLModule.Contracts.Training.Skill;

/// <summary>Создание преподавательского навыка.</summary>
public sealed record CreateSkillRequest(string? SkillId, string? Name, string? Description = null);
