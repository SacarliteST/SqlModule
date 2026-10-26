using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Bank;

/// <summary>Общие правила наличия содержательной автоматической подсказки.</summary>
public static class TaskBankHintRules
{
    /// <summary>Проверяет, что хотя бы одна включённая группа подкреплена критерием.</summary>
    public static bool HasAutomaticHint(
        IEnumerable<HintGroup> visibleGroups,
        IEnumerable<ValidationCheckKind> checkKinds)
    {
        var visible = visibleGroups.ToHashSet();
        return checkKinds.Any(kind => ToHintGroup(kind) is { } group && visible.Contains(group));
    }

    private static HintGroup? ToHintGroup(ValidationCheckKind kind) => kind switch
    {
        ValidationCheckKind.MainDatasetResult => HintGroup.Result,
        ValidationCheckKind.RequiredConstruct => HintGroup.RequiredConstructs,
        ValidationCheckKind.ForbiddenConstruct => HintGroup.ForbiddenConstructs,
        ValidationCheckKind.RequiredTable => HintGroup.RequiredTables,
        ValidationCheckKind.ForbiddenTable => HintGroup.ForbiddenTables,
        _ => null
    };
}
