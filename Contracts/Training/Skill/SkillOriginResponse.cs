using System.Text.Json.Serialization;

namespace SQLModule.Contracts.Training.Skill;

/// <summary>Источник определения навыка.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SkillOriginResponse>))]
public enum SkillOriginResponse
{
    System = 1,
    TeacherDefined = 2
}
