namespace SQLModule.Web.Common;

/// <summary>Ключи конфигурации для изолированного режима (флаги, дефолт = false).</summary>
public static class IsolatedKeys
{
    /// <summary>Включить фейковую аутентификацию Robot вместо JwtBearer (без Identity).</summary>
    public const string UseRobotAuth = nameof(UseRobotAuth);

    /// <summary>Включить фейковую песочницу без Docker.</summary>
    public const string UseFakeSandbox = nameof(UseFakeSandbox);

    /// <summary>Разрешить CORS для любых origin (для дев-фронта).</summary>
    public const string UseAllowAllCors = nameof(UseAllowAllCors);

    /// <summary>Засеять демо-данные при старте (идемпотентно, только в изолированном режиме).</summary>
    public const string SeedDemoData = nameof(SeedDemoData);

    /// <summary>Создать стабильный минимальный набор для platform smoke-теста.</summary>
    public const string SeedSmokeData = nameof(SeedSmokeData);

    /// <summary>Пропустить миграции при запуске tooling, которому нужен только граф сервисов (например, Swagger CLI).</summary>
    public const string SkipDatabaseInitialization = nameof(SkipDatabaseInitialization);
}
