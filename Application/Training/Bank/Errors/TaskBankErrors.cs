using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Bank;

/// <summary>Ошибки проверки готовности задания к открытому банку.</summary>
public static class TaskBankErrors
{
    /// <summary>Задание ещё не опубликовано.</summary>
    public static Error NotPublished { get; } = Error.Conflict(
        "TaskBank.NotPublished",
        "Сначала опубликуйте задание.");

    /// <summary>У задания отсутствует существующая тема.</summary>
    public static Error TopicMissing { get; } = Error.Conflict(
        "TaskBank.TopicMissing",
        "У задания должна быть существующая тема.");

    /// <summary>У задания отсутствует условие.</summary>
    public static Error TaskTextMissing { get; } = Error.Conflict(
        "TaskBank.TaskTextMissing",
        "Заполните условие задания.");

    /// <summary>Для задания нельзя сформировать доступную студенту подсказку.</summary>
    public static Error HintMissing { get; } = Error.Conflict(
        "TaskBank.HintMissing",
        "Добавьте авторскую подсказку или включите содержательную автоматическую подсказку.");
}
