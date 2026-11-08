namespace SQLModule.Contracts.Training.Skill;

/// <summary>Навык SQL-модуля.</summary>
public sealed record SkillResponse(
    Guid Id,
    string SkillId,
    string Name,
    string? Description,
    SkillOriginResponse Origin,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string SemanticChangeWarning);
