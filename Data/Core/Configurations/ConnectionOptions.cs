namespace SQLModule.Data.Core.Configurations;

/// <summary>
/// Параметры подключения к базе данных
/// </summary>
public sealed class ConnectionOptions
{
    /// <summary>
    /// Ключ параметров
    /// </summary>
    public const string OptionsKey = "ConnectionStrings";

    /// <summary>
    /// Строка подключения
    /// </summary>
    public string? ConnectionString { get; set; }
}
