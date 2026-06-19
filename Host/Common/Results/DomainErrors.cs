using System.Runtime.CompilerServices;

namespace SQLModule.Host.Common.Results;

/// <summary>
/// Генерик-фабрика ошибок с автоматическим кодом вида <c>{Entity}.{Reason}</c>.
/// {Entity} = typeof(TEntity).Name; {Reason} = имя вызвавшего члена каталога (<see cref="CallerMemberNameAttribute"/>).
/// Не используется в хендлерах напрямую — только через каталоги ошибок (<c>XxxErrors</c>).
/// </summary>
public static class DomainErrors<TEntity>
{
    private static string Prefix => typeof(TEntity).Name;

    /// <summary>Ошибка 404: адресуемая сущность не найдена.</summary>
    public static Error NotFound(object id, [CallerMemberName] string reason = "")
        => new($"{Prefix}.{reason}", $"{Prefix} с id '{id}' не найден(а).", ErrorType.NotFound);

    /// <summary>Ошибка 409: конфликт состояния (нарушение FK, занятость ресурса).</summary>
    public static Error Conflict(string message, [CallerMemberName] string reason = "")
        => new($"{Prefix}.{reason}", message, ErrorType.Conflict);

    /// <summary>Ошибка 422: нарушение бизнес-правила ввода.</summary>
    public static Error Validation(string message, [CallerMemberName] string reason = "")
        => new($"{Prefix}.{reason}", message, ErrorType.Validation);
}
