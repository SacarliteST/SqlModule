using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary;

internal static class DbmsDictionaryMappings
{
    internal static DbmsDictionaryResponse ToResponse(Domain.DbmsCatalog.DbmsDictionary e) => new(
        e.Id, e.DbmsName, e.DbmsSystemName, e.DockerImage, e.DefaultPort,
        e.EnvUserKey, e.EnvPasswordKey, e.EnvDatabaseKey, e.ExtraEnvConfig,
        e.DefaultDatabase, e.DefaultUsername,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static DbmsProbeSpec ToProbeSpec(CreateDbmsDictionaryRequest req) => new(
        req.DbmsSystemName, req.DockerImage, req.DefaultPort,
        req.EnvUserKey, req.EnvPasswordKey, req.EnvDatabaseKey, req.ExtraEnvConfig,
        req.DefaultDatabase, req.DefaultUsername, req.DefaultPassword);

    internal static DbmsProbeSpec ToProbeSpec(UpdateDbmsDictionaryRequest req) => new(
        req.DbmsSystemName, req.DockerImage, req.DefaultPort,
        req.EnvUserKey, req.EnvPasswordKey, req.EnvDatabaseKey, req.ExtraEnvConfig,
        req.DefaultDatabase, req.DefaultUsername, req.DefaultPassword);
}
