namespace SQLModule.Client;

/// <summary>Ресурс не найден (404). Бросается при Update несуществующего ресурса.</summary>
public sealed class NotFoundException : ApiException
{
    /// <inheritdoc cref="ApiException(Int32, ApiProblem?)"/>
    public NotFoundException(int statusCode, ApiProblem? problem) : base(statusCode, problem) { }
}
