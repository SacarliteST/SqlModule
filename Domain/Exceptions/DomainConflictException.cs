namespace SQLModule.Domain.Exceptions;

/// <summary>
/// Нарушено бизнес-правило состояния, обнаруженное там, где Result недоступен (например, при сохранении).
/// Сопоставляется с 409 Conflict и стабильным machine-кодом.
/// </summary>
public sealed class DomainConflictException(
    string code,
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(message)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}
