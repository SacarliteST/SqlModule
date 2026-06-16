namespace TemplateApi.Client;

/// <summary>
/// Ошибка при получении данных от сервера, не соответствующих ожидаемому формату
/// </summary>
public sealed class InvalidResponseFormatException : InvalidOperationException
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public InvalidResponseFormatException() : base("Формат ответа не соответствует ожидаемому") { }
}
