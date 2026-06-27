namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

internal sealed record DbmsProbeSpec(
    string DockerImage,
    int DefaultPort,
    string EnvUserKey,
    string EnvPasswordKey,
    string EnvDatabaseKey,
    string? ExtraEnvConfig,
    string DefaultDatabase,
    string DefaultUsername,
    string DefaultPassword);
