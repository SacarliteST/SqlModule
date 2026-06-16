namespace TemplateApi.Client.Configurations;

/// <summary>
/// Опции хранения url серверной части
/// </summary>
public sealed class TemplateClientOptions
{
    /// <summary>
    /// Ключ параметров
    /// </summary>
    public const string OptionsKey = "TemplateApp";

    /// <summary>
    /// API URL
    /// </summary>
    public required Uri ServerUrl { get; set; }
}
