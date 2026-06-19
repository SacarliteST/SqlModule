namespace SQLModule.Client;

/// <summary>Опции подключения к SQLModule API.</summary>
public sealed class SqlModuleClientOptions
{
    /// <summary>Ключ секции в конфигурации.</summary>
    public const string SectionKey = "SqlModule";

    /// <summary>
    /// Базовый адрес API. Должен заканчиваться на '/'.
    /// Пример: <c>https://api.example.com/</c>.
    /// </summary>
    public required Uri BaseAddress { get; set; }
}
