using System.Text.Json;

namespace SQLModule.Client;

/// <summary>Общие настройки JSON для всех HTTP-клиентов.</summary>
internal static class ClientJson
{
    /// <summary>Web-дефолты: camelCase + PropertyNameCaseInsensitive.</summary>
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
