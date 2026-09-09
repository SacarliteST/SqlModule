namespace SQLModule.Client;

/// <summary>DTO описания ошибки RFC 7807 (ProblemDetails), без зависимости на ASP.NET MVC.</summary>
public sealed record ApiProblem(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    IDictionary<string, string[]>? Errors,
    string? Code,
    IReadOnlyList<ApiViolation>? Violations = null);

/// <summary>Структурированное нарушение конкретного поля или элемента запроса.</summary>
/// <param name="Path">Стабильный путь к полю.</param>
/// <param name="Code">Машинный код нарушения.</param>
/// <param name="Message">Сообщение для пользователя.</param>
/// <param name="Severity">Уровень серьёзности.</param>
/// <param name="AffectedRows">Количество затронутых строк, если применимо.</param>
/// <param name="Limit">Числовой лимит, нарушенный значением поля.</param>
public sealed record ApiViolation(
    string Path,
    string Code,
    string Message,
    string Severity,
    long? AffectedRows = null,
    long? Limit = null);
