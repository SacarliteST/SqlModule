using SQLModule.Domain.Common;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Domain.Training;

/// <summary>Редактируемая конфигурация проверки SQL-задания.</summary>
public sealed class TaskValidationConfiguration : AuditableEntity
{
    private readonly List<ValidationCheck> checks = [];
    private readonly List<TaskValidationSkillTag> skillTags = [];

    public Guid TaskId { get; private set; }
    public int PassingScore { get; private set; }
    public long VisibleHintGroupsMask { get; private set; }
    public Guid Version { get; private set; }

    public SqlTask Task { get; private set; } = null!;
    public IReadOnlyCollection<ValidationCheck> Checks => checks.AsReadOnly();
    public IReadOnlyCollection<TaskValidationSkillTag> SkillTags => skillTags.AsReadOnly();

    private TaskValidationConfiguration(
        Guid id,
        Guid taskId,
        int passingScore,
        long visibleHintGroupsMask) : base(id)
    {
        TaskId = taskId;
        PassingScore = passingScore;
        VisibleHintGroupsMask = visibleHintGroupsMask;
        Version = Guid.NewGuid();
    }

    public static TaskValidationConfiguration Create(
        Guid taskId,
        int passingScore,
        IEnumerable<HintGroup> visibleHintGroups,
        Guid? id = null) =>
        new(id ?? Guid.NewGuid(), taskId, passingScore, ToMask(visibleHintGroups));

    public void UpdateSettings(
        int passingScore,
        IEnumerable<HintGroup> visibleHintGroups)
    {
        PassingScore = passingScore;
        VisibleHintGroupsMask = ToMask(visibleHintGroups);
        Touch();
    }

    public void SynchronizeChecks(IReadOnlyList<ValidationCheckDefinition> definitions)
    {
        var requestedIds = definitions
            .Where(definition => definition.Id.HasValue)
            .Select(definition => definition.Id!.Value)
            .ToHashSet();

        checks.RemoveAll(check => !requestedIds.Contains(check.Id));

        foreach (var definition in definitions)
        {
            var existing = definition.Id.HasValue
                ? checks.SingleOrDefault(check => check.Id == definition.Id.Value)
                : null;
            if (existing is null)
            {
                checks.Add(ValidationCheck.Create(
                    Id,
                    definition.Kind,
                    definition.Value,
                    definition.Weight,
                    definition.Order,
                    definition.Id));
            }
            else
            {
                existing.Update(definition.Kind, definition.Value, definition.Weight, definition.Order);
            }
        }
    }

    public void SynchronizeSkillTags(IReadOnlyCollection<Guid> skillIds)
    {
        var requested = skillIds.ToHashSet();
        skillTags.RemoveAll(tag => !requested.Contains(tag.SkillId));
        foreach (var skillId in requested.Where(skillId => skillTags.All(tag => tag.SkillId != skillId)))
        {
            skillTags.Add(TaskValidationSkillTag.Create(Id, skillId));
        }
    }

    public IReadOnlyList<HintGroup> GetVisibleHintGroups() =>
        Enum.GetValues<HintGroup>()
            .Where(group => (VisibleHintGroupsMask & (1L << (int)group)) != 0)
            .ToArray();

    public void Touch() => Version = Guid.NewGuid();

    private static long ToMask(IEnumerable<HintGroup> groups) =>
        groups.Distinct().Aggregate(0L, (mask, group) => mask | (1L << (int)group));
}
