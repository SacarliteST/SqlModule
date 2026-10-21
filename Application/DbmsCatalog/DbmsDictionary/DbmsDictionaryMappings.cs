using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Sandbox.DbmsProbing;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary;

public static class DbmsDictionaryMappings
{
    public static DbmsDictionaryResponse ToResponse(Domain.DbmsCatalog.DbmsDictionary e) => new(
        e.Id, e.DbmsName, e.DbmsSystemName, e.DockerImage, e.DefaultPort,
        e.EnvUserKey, e.EnvPasswordKey, e.EnvDatabaseKey, e.ExtraEnvConfig,
        e.DefaultDatabase, e.DefaultUsername,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static DbmsProbeSpec ToProbeSpec(CreateDbmsDictionaryRequest req) => new(
        req.DbmsSystemName, req.DockerImage, req.DefaultPort,
        req.EnvUserKey, req.EnvPasswordKey, req.EnvDatabaseKey, req.ExtraEnvConfig,
        req.DefaultDatabase, req.DefaultUsername, req.DefaultPassword);

    public static DbmsProbeSpec ToProbeSpec(UpdateDbmsDictionaryRequest req) => new(
        req.DbmsSystemName, req.DockerImage, req.DefaultPort,
        req.EnvUserKey, req.EnvPasswordKey, req.EnvDatabaseKey, req.ExtraEnvConfig,
        req.DefaultDatabase, req.DefaultUsername, req.DefaultPassword);
}
