using System.Runtime.CompilerServices;

namespace TemplateApi.Common.Extensions;

/// <summary>
/// Расширения для работы с объектами
/// </summary>
public static class ObjectExtension
{
    /// <summary>
    /// Возвращает значение или выбрасывает <see cref="ArgumentNullException"/>,
    /// если оно равно null (ссылочный тип)
    /// </summary>
    public static T Required<T>(
        this T? value,
        [CallerArgumentExpression("value")] string? paramName = default)
        where T : class
        => value ?? throw new ArgumentNullException(paramName);

    /// <summary>
    /// Возвращает значение или выбрасывает <see cref="ArgumentNullException"/>,
    /// если оно равно null (значимый тип)
    /// </summary>
    public static T Required<T>(
        this T? value,
        [CallerArgumentExpression("value")] string? paramName = default)
        where T : struct
        => value ?? throw new ArgumentNullException(paramName);

    /// <summary>
    /// Оборачивает значение в <see cref="Task{T}"/>
    /// </summary>
    public static Task<T> AsTask<T>(this T value)
        => Task.FromResult(value);

    /// <summary>
    /// Фильтрует последовательность, исключая null-элементы (ссылочный тип)
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
        => from item in source
           where item is not null
           select item;

    /// <summary>
    /// Фильтрует последовательность, исключая null-элементы (значимый тип)
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : struct
        => from item in source
           where item is not null
           select item.Value;
}
