namespace SQLModule.Client;

/// <summary>Конфликт состояния (409): нарушение FK-зависимости или дублирование ресурса.</summary>
public sealed class ConflictException : ApiException
{
    /// <inheritdoc cref="ApiException(int, ApiProblem?)"/>
    public ConflictException(int statusCode, ApiProblem? problem) : base(statusCode, problem) { }
}
