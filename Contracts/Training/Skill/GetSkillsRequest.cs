namespace SQLModule.Contracts.Training.Skill;

/// <summary>Фильтры каталога навыков.</summary>
public sealed record GetSkillsRequest(
    string? Search = null,
    SkillOriginResponse? Origin = null,
    bool IncludeDeleted = false,
    int Offset = 0,
    int Limit = 20);
