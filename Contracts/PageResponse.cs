namespace Contracts;

/// <summary>
/// Ответ в виде страницы с информацией о элементах и их кол-ве
/// </summary>
public class PageResponse<T>
{
    /// <summary>
    /// Количество элементов
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Список элементов
    /// </summary>
    public List<T> Items { get; set; } = new();
}
