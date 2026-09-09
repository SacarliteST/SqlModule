namespace SQLModule.Common.Results;

/// <summary>
/// Категория ошибки. Используется для маппинга на HTTP-статус в <see cref="ResultExtensions"/>.
/// </summary>
public enum ErrorType
{
    /// <summary>Нарушение бизнес-правила ввода (422).</summary>
    Validation,

    /// <summary>Сущность не найдена (404).</summary>
    NotFound,

    /// <summary>Доступ к найденному ресурсу запрещён (403).</summary>
    Forbidden,

    /// <summary>Конфликт состояния (409).</summary>
    Conflict,

    /// <summary>Условие конкурентного обновления не выполнено (412).</summary>
    PreconditionFailed,

    /// <summary>Внешний движок или инфраструктура временно недоступны (503).</summary>
    Unavailable,

    /// <summary>Внутренняя ошибка (500).</summary>
    Failure
}

/// <summary>
/// Описание доменной ошибки. Передаётся внутри <see cref="Result"/> или <see cref="Result{T}"/>.
/// </summary>
/// <param name="Code">Уникальный код ошибки вида <c>Entity.Reason</c>.</param>
/// <param name="Message">Читаемое сообщение для клиента.</param>
/// <param name="Type">Категория ошибки.</param>
public record Error(
    string Code,
    string Message,
    ErrorType Type,
    string? Path = null,
    long? AffectedRows = null,
    string Severity = "Error",
    long? Limit = null)
{
    /// <summary>Создаёт ошибку нарушения бизнес-правила (422).</summary>
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    /// <summary>Создаёт ошибку 422 с путём поля и связанным числовым ограничением.</summary>
    public static Error Validation(string code, string message, string path, long affectedRows) =>
        new(code, message, ErrorType.Validation, path, Limit: affectedRows);

    /// <summary>Создаёт ошибку 404 для сущности с указанным идентификатором.</summary>
    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} с id '{id}' не найден(а).", ErrorType.NotFound);

    /// <summary>Создаёт ошибку конфликта состояния (409).</summary>
    public static Error Conflict(
        string code, string message, string? path = null, long? affectedRows = null) =>
        new(code, message, ErrorType.Conflict, path, affectedRows);

    /// <summary>Создаёт ошибку запрета доступа (403).</summary>
    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    /// <summary>Создаёт ошибку устаревшей версии ресурса (412).</summary>
    public static Error PreconditionFailed(string code, string message) =>
        new(code, message, ErrorType.PreconditionFailed);

    public static Error Unavailable(string code, string message) =>
        new(code, message, ErrorType.Unavailable);

    /// <summary>Создаёт внутреннюю ошибку (500).</summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
