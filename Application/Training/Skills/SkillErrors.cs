using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Skills;

public static class SkillErrors
{
    public static Error NotFound(Guid id) => DomainErrors<Skill>.NotFound(id);

    public static Error AlreadyExists(string skillId) =>
        DomainErrors<Skill>.Conflict($"Навык с идентификатором '{skillId}' уже существует.");

    public static Error AlreadyDeleted(Guid id) =>
        DomainErrors<Skill>.Conflict($"Навык '{id}' уже удалён.");

    public static Error NotDeleted(Guid id) =>
        DomainErrors<Skill>.Conflict($"Навык '{id}' не удалён.");
}
