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
}
