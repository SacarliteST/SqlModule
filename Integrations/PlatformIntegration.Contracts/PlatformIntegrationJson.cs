using System.Text.Json;

namespace SQLModule.PlatformIntegration.Contracts;

public static class PlatformIntegrationJson
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
}
