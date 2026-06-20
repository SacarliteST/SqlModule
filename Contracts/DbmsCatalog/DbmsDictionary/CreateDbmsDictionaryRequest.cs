namespace SQLModule.Contracts.DbmsCatalog.DbmsDictionary;

public record CreateDbmsDictionaryRequest(
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
    string DefaultPassword);
