using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;

namespace SQLModule.Application.TaskMaterialization;

/// <summary>
/// Маппинг доменной сущности <see cref="DbmsDictionary"/> на <see cref="SandboxDbmsSpec"/>.
/// Boundary-слой: Sandbox не знает о Domain.
/// </summary>
public static class SandboxSpecMapping
{
    public static SandboxDbmsSpec ToSandboxSpec(this DbmsDictionary dbms) =>
        new(
            SystemName: dbms.DbmsSystemName,
            DockerImage: dbms.DockerImage,
            DefaultPort: dbms.DefaultPort,
            EnvUserKey: dbms.EnvUserKey,
            DefaultUsername: dbms.DefaultUsername,
            EnvPasswordKey: dbms.EnvPasswordKey,
            DefaultPassword: dbms.DefaultPassword,
            EnvDatabaseKey: dbms.EnvDatabaseKey,
            DefaultDatabase: dbms.DefaultDatabase,
            ExtraEnvConfig: dbms.ExtraEnvConfig);
}
