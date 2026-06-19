namespace SQLModule.Client;

/// <summary>Ошибка валидации запроса (400). Содержит словарь ошибок по полям.</summary>
public sealed class ValidationException : ApiException
{
    /// <summary>Ошибки по полям запроса. Ключ — имя поля, значение — сообщения валидатора.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <inheritdoc cref="ApiException(Int32, ApiProblem?)"/>
    public ValidationException(int statusCode, ApiProblem? problem)
        : base(statusCode, problem)
    {
        Errors = problem?.Errors is not null
            ? new Dictionary<string, string[]>(problem.Errors)
            : new Dictionary<string, string[]>();
    }
}
