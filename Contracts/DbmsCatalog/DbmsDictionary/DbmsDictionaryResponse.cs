namespace SQLModule.Contracts.DbmsCatalog.DbmsDictionary;

public record DbmsDictionaryResponse(
    Guid Id,
    string DbmsName,
    string DbmsSystemName,
    string DockerImage,
    int DefaultPort,
    string EnvUserKey,
    string EnvPasswordKey,
    string EnvDatabaseKey,
    string? ExtraEnvConfig,
    string DefaultDatabase,
    string DefaultUsername,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    Guid UpdatedById,
    DateTimeOffset UpdatedAt);
