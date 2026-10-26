namespace SQLModule.Common.Results;

/// <summary>
/// Описание доменной ошибки. Передаётся внутри <see cref="Result"/> или <see cref="Result{T}"/>.
/// </summary>
/// <param name="Code">Уникальный код ошибки вида <c>Entity.Reason</c>.</param>
/// <param name="Message">Читаемое сообщение для клиента.</param>
/// <param name="Type">Категория ошибки.</param>
/// <param name="Errors">Дополнительные причины по ключу (например, код) — попадают в поле <c>errors</c> ProblemDetails.</param>
public record Error(
    string Code,
    string Message,
    ErrorType Type,
    string? Path = null,
    long? AffectedRows = null,
    string Severity = "Error",
    long? Limit = null,
    IReadOnlyDictionary<string, string[]>? Errors = null)
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

    /// <summary>Создаёт ошибку ограничения частоты запросов (429).</summary>
    public static Error TooManyRequests(string code, string message, long retryAfterSeconds) =>
        new(code, message, ErrorType.TooManyRequests, Limit: retryAfterSeconds);

    /// <summary>Создаёт внутреннюю ошибку (500).</summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
