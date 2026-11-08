namespace SQLModule.Domain.Training;

/// <summary>Источник определения навыка SQL-модуля.</summary>
public enum SkillOrigin
{
    /// <summary>Навык поставляется разработчиками модуля.</summary>
    System = 1,

    /// <summary>Навык создан преподавателем.</summary>
    TeacherDefined = 2
}
