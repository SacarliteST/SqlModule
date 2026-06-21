namespace SQLModule.Sandbox;

/// <summary>
/// Снимок Docker-конфигурации СУБД для запуска песочницы.
/// Не зависит от доменной сущности DbmsDictionary — маппинг выполняется на границе Host.
/// </summary>
public sealed record SandboxDbmsSpec(
    string SystemName,
    string DockerImage,
    int DefaultPort,
    string EnvUserKey,
    string DefaultUsername,
    string EnvPasswordKey,
    string DefaultPassword,
    string? EnvDatabaseKey,
    string DefaultDatabase,
    string? ExtraEnvConfig);
