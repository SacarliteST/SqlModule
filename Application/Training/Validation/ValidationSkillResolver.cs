using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;
using SQLModule.Application.SpdIntegration;
using SQLModule.Common.Results;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Application.Training.Validation;

public sealed record ValidationSkillDefinition(string SkillId, string Name, string Description);

/// <summary>Назначает системные навыки весовым правилам и проверяет навыки-теги.</summary>
public sealed class ValidationSkillResolver(IApplicationDbContext db, SkillCatalogOutbox outbox)
{
    public async Task<Result<IReadOnlyList<Skill>>> EnsureAsync(
        TaskValidationDefinition definition,
        CancellationToken ct)
    {
        var ruleDefinitions = definition.Checks.Select(Resolve).ToArray();
        var duplicate = ruleDefinitions.GroupBy(value => value.SkillId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            return Result<IReadOnlyList<Skill>>.Fail(
                TaskValidationErrors.DuplicateSkill(duplicate.Key, "checks"));
        }

        var externalIds = ruleDefinitions.Select(value => value.SkillId).ToArray();
        var storedSkills = await db.Skills
            .Where(skill => externalIds.Contains(skill.SkillId))
            .ToListAsync(ct);
        var skills = storedSkills
            .Concat(db.Skills.Local.Where(skill => externalIds.Contains(skill.SkillId)))
            .DistinctBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToList();
        foreach (var missing in ruleDefinitions.Where(definition =>
                     skills.All(skill => skill.SkillId != definition.SkillId)))
        {
            var skill = Skill.CreateSystem(missing.SkillId, missing.Name, missing.Description);
            db.Skills.Add(skill);
            outbox.Add(skill);
            skills.Add(skill);
        }

        var deletedRuleSkill = skills.FirstOrDefault(skill => skill.IsDeleted);
        if (deletedRuleSkill is not null)
        {
            return Result<IReadOnlyList<Skill>>.Fail(
                TaskValidationErrors.RuleSkillUnavailable(deletedRuleSkill.SkillId));
        }

        var tagIds = definition.EffectiveSkillTagIds;
        if (tagIds.Count != tagIds.Distinct().Count())
        {
            return Result<IReadOnlyList<Skill>>.Fail(TaskValidationErrors.DuplicateSkillTag());
        }

        var tags = await db.Skills.Where(skill => tagIds.Contains(skill.Id)).ToListAsync(ct);
        var unavailable = tagIds.FirstOrDefault(id => tags.All(skill => skill.Id != id || skill.IsDeleted));
        if (unavailable != Guid.Empty)
        {
            return Result<IReadOnlyList<Skill>>.Fail(TaskValidationErrors.SkillUnavailable(unavailable));
        }

        var ruleSkillIds = externalIds.ToHashSet(StringComparer.Ordinal);
        var conflict = tags.FirstOrDefault(skill => ruleSkillIds.Contains(skill.SkillId));
        if (conflict is not null)
        {
            return Result<IReadOnlyList<Skill>>.Fail(
                TaskValidationErrors.DuplicateSkill(conflict.SkillId, "skillTagIds"));
        }

        return skills.Concat(tags).DistinctBy(skill => skill.Id).ToArray();
    }

    public static ValidationSkillDefinition Resolve(ValidationCheckDefinition check)
    {
        var suffix = check.Kind == ValidationCheckKind.MainDatasetResult
            ? "main-dataset-result"
            : Normalize(check.Value);
        var skillId = $"validation.{check.Kind.ToString().ToLowerInvariant()}.{suffix}";
        return new ValidationSkillDefinition(
            skillId[..Math.Min(100, skillId.Length)],
            GetName(check),
            "Системный навык автоматически назначен весовому правилу SQL-модуля.");
    }

    private static string Normalize(string? value) =>
        String.IsNullOrWhiteSpace(value)
            ? "none"
            : value.Trim().ToLowerInvariant().Replace("_", "-", StringComparison.Ordinal);

    private static string GetName(ValidationCheckDefinition check) => check.Kind switch
    {
        ValidationCheckKind.MainDatasetResult => "Корректный результат запроса",
        ValidationCheckKind.RequiredConstruct => $"Использование конструкции {check.Value}",
        ValidationCheckKind.ForbiddenConstruct => $"Отказ от конструкции {check.Value}",
        ValidationCheckKind.RequiredTable => $"Использование обязательной таблицы {check.Value}",
        ValidationCheckKind.ForbiddenTable => $"Исключение запрещённой таблицы {check.Value}",
        _ => check.Kind.ToString()
    };
}
