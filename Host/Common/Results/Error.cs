namespace SQLModule.Host.Common.Results;

/// <summary>
/// Категория ошибки. Используется для маппинга на HTTP-статус в <see cref="ResultExtensions"/>.
/// </summary>
public enum ErrorType
{
    /// <summary>Внутренняя ошибка (500).</summary>
    Failure,

    /// <summary>Сущность не найдена (404).</summary>
    NotFound,

    /// <summary>Конфликт состояния (409).</summary>
    Conflict
}

/// <summary>
/// Описание доменной ошибки. Передаётся внутри <see cref="Result"/> или <see cref="Result{T}"/>.
/// </summary>
/// <param name="Code">Уникальный код ошибки вида <c>Entity.Reason</c>.</param>
/// <param name="Message">Читаемое сообщение для клиента.</param>
/// <param name="Type">Категория ошибки.</param>
public record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Создаёт ошибку 404 для сущности с указанным идентификатором.</summary>
    /// <param name="entity">Имя сущности (например, <c>TargetDb</c>).</param>
    /// <param name="id">Идентификатор сущности.</param>
    public static Error NotFound(string entity, object id) =>
        new($"{entity}.NotFound", $"{entity} with id '{id}' was not found.", ErrorType.NotFound);

    /// <summary>Создаёт ошибку конфликта состояния (409).</summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Сообщение.</param>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>Создаёт внутреннюю ошибку (500).</summary>
    /// <param name="code">Код ошибки.</param>
    /// <param name="message">Сообщение.</param>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
