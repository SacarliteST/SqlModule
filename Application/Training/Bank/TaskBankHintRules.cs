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

    /// <summary>Возвращает группу подсказки для вида критерия.</summary>
    public static HintGroup? ToHintGroup(ValidationCheckKind kind) => kind switch
    {
        ValidationCheckKind.MainDatasetResult => HintGroup.Result,
        ValidationCheckKind.RequiredConstruct => HintGroup.RequiredConstructs,
        ValidationCheckKind.ForbiddenConstruct => HintGroup.ForbiddenConstructs,
        ValidationCheckKind.RequiredTable => HintGroup.RequiredTables,
        ValidationCheckKind.ForbiddenTable => HintGroup.ForbiddenTables,
        _ => null
    };

    /// <summary>Возвращает безопасный текст автоматической подсказки.</summary>
    public static string MessageFor(HintGroup group) => group switch
    {
        HintGroup.Result => "Результат запроса пока не совпадает с ожидаемым.",
        HintGroup.RequiredConstructs => "Проверьте наличие обязательных SQL-конструкций.",
        HintGroup.ForbiddenConstructs => "Проверьте, не использованы ли запрещённые SQL-конструкции.",
        HintGroup.RequiredTables => "Проверьте использование обязательных таблиц.",
        HintGroup.ForbiddenTables => "Проверьте, не использованы ли запрещённые таблицы.",
        _ => "Проверьте соответствующую часть решения."
    };
}
