using SQLModule.Common.Results;

namespace SQLModule.Web.Features.Training.Validation;

internal static class TaskValidationErrors
{
    internal static Error TaskNotFound(Guid taskId) => Error.NotFound("SqlTask", taskId);

    internal static Error ConfigurationNotFound(Guid taskId) =>
        Error.NotFound("TaskValidationConfiguration", taskId);

    internal static Error StaleVersion() =>
        Error.Conflict(
            "TaskValidation.StaleVersion",
            "Конфигурация уже изменена. Обновите данные и повторите операцию.");

    internal static Error CheckNotFound(Guid checkId) =>
        Error.Validation(
            "Validation.CheckNotFound",
            $"Критерий '{checkId}' не принадлежит текущей конфигурации.");

    internal static Error UnsupportedAnalyzer(string systemName) =>
        Error.Validation(
            "Validation.AnalyzerNotSupported",
            $"Проверка SQL для СУБД '{systemName}' пока не поддерживается.");

    internal static Error ReferenceInvalid(string code, string message, string path = "referenceQuery.sqlText") =>
        new(code, message, ErrorType.Validation, path);

    internal static Error ReferenceDoesNotScore100(string message, string path = "checks") =>
        new("Validation.ReferenceMustScore100", message, ErrorType.Validation, path);

    internal static Error InfrastructureUnavailable() =>
        Error.Unavailable(
            "TaskValidation.InfrastructureUnavailable",
            "Сервис проверки SQL временно недоступен. Повторите попытку позже.");
}
