using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Validation;

public static class TaskValidationErrors
{
    public static Error TaskNotFound(Guid taskId) => Error.NotFound("SqlTask", taskId);

    public static Error ConfigurationNotFound(Guid taskId) =>
        Error.NotFound("TaskValidationConfiguration", taskId);

    public static Error StaleVersion() =>
        Error.Conflict(
            "TaskValidation.StaleVersion",
            "Конфигурация уже изменена. Обновите данные и повторите операцию.");

    public static Error CheckNotFound(Guid checkId) =>
        Error.Validation(
            "Validation.CheckNotFound",
            $"Критерий '{checkId}' не принадлежит текущей конфигурации.");

    public static Error UnsupportedAnalyzer(string systemName) =>
        Error.Validation(
            "Validation.AnalyzerNotSupported",
            $"Проверка SQL для СУБД '{systemName}' пока не поддерживается.");

    public static Error ReferenceInvalid(string code, string message, string path = "referenceQuery.sqlText") =>
        new(code, message, ErrorType.Validation, path);

    public static Error ReferenceDoesNotScore100(string message, string path = "checks") =>
        new("Validation.ReferenceMustScore100", message, ErrorType.Validation, path);

    public static Error InfrastructureUnavailable() =>
        Error.Unavailable(
            "TaskValidation.InfrastructureUnavailable",
            "Сервис проверки SQL временно недоступен. Повторите попытку позже.");

    public static Error SkillUnavailable(Guid skillId) =>
        new Error(
            "Validation.SkillUnavailable",
            $"Навык-тег '{skillId}' не найден или удалён.",
            ErrorType.Validation,
            "skillTagIds");

    public static Error RuleSkillUnavailable(string skillId) =>
        new Error(
            "Validation.RuleSkillUnavailable",
            $"Системный навык '{skillId}' удалён и не может быть закреплён за правилом.",
            ErrorType.Validation,
            "checks");

    public static Error DuplicateSkillTag() =>
        new Error(
            "Validation.DuplicateSkillTag",
            "Навык-тег указан повторно.",
            ErrorType.Validation,
            "skillTagIds");

    public static Error DuplicateSkill(string skillId, string path) =>
        new Error(
            "Validation.DuplicateSkill",
            $"Навык '{skillId}' встречается в задании более одного раза.",
            ErrorType.Validation,
            path);
}
