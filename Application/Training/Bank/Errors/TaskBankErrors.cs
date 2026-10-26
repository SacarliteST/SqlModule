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

    /// <summary>Студент ещё не отправил неуспешное тренировочное решение.</summary>
    public static Error HintAttemptRequired { get; } = Error.Conflict(
        "TaskBank.HintAttemptRequired",
        "Сначала отправьте решение.");

    /// <summary>Разбор отключён преподавателем.</summary>
    public static Error SolutionDisabled { get; } = Error.Conflict(
        "TaskBank.SolutionDisabled",
        "Разбор решения для этого задания недоступен.");

    /// <summary>Студент ещё не решил задание.</summary>
    public static Error SolutionAttemptRequired { get; } = Error.Conflict(
        "TaskBank.SolutionAttemptRequired",
        "Разбор станет доступен после успешного решения задания.");
}
