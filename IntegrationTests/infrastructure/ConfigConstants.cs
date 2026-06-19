namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>Ключи конфигурации, используемые при подмене настроек тест-сервера.</summary>
internal static class ConfigConstants
{
    /// <summary>Строка подключения к БД.</summary>
    public const string DbConnection = "ConnectionStrings:ConnectionString";

    /// <summary>Тип провайдера БД.</summary>
    public const string DbProvider = "ConnectionStrings:DbProvider";
}
