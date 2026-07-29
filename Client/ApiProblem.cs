namespace SQLModule.Client;

/// <summary>DTO описания ошибки RFC 7807 (ProblemDetails), без зависимости на ASP.NET MVC.</summary>
public sealed record ApiProblem(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    IDictionary<string, string[]>? Errors,
    string? Code);
