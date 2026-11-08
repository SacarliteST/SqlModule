namespace SQLModule.Contracts.Training.Skill;

/// <summary>Изменение отображаемых данных навыка.</summary>
public sealed record UpdateSkillRequest(string? Name, string? Description = null);
