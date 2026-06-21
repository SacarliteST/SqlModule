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

    /// <summary>Конфликт состояния (409).</summary>
    Conflict,

    /// <summary>Внутренняя ошибка (500).</summary>
    Failure
}

/// <summary>
/// Описание доменной ошибки. Передаётся внутри <see cref="Result"/> или <see cref="Result{T}"/>.
/// </summary>
/// <param name="Code">Уникальный код ошибки вида <c>Entity.Reason</c>.</param>
/// <param name="Message">Читаемое сообщение для клиента.</param>
/// <param name="Type">Категория ошибки.</param>
public record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Создаёт ошибку нарушения бизнес-правила (422).</summary>
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    /// <summary>Создаёт ошибку 404 для сущности с указанным идентификатором.</summary>
    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} с id '{id}' не найден(а).", ErrorType.NotFound);

    /// <summary>Создаёт ошибку конфликта состояния (409).</summary>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>Создаёт внутреннюю ошибку (500).</summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
