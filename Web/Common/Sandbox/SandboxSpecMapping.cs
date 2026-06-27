using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Sandbox;

/// <summary>
/// Маппинг доменной сущности <see cref="DbmsDictionary"/> на <see cref="SandboxDbmsSpec"/>.
/// Boundary-слой: Sandbox не знает о Domain.
/// </summary>
internal static class SandboxSpecMapping
{
    internal static SandboxDbmsSpec ToSandboxSpec(this DbmsDictionary dbms) =>
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
