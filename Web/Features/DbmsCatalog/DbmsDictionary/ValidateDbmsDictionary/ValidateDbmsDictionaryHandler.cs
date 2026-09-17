using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.ValidateDbmsDictionary;

internal record ValidateDbmsDictionaryCommand(
    string DbmsSystemName,
    string DockerImage,
    int DefaultPort,
    string EnvUserKey,
    string EnvPasswordKey,
    string EnvDatabaseKey,
    string? ExtraEnvConfig,
    string DefaultDatabase,
    string DefaultUsername,
    string DefaultPassword) : IRequest<Result>;

internal sealed class ValidateDbmsDictionaryHandler(IDbmsProbe probe)
    : IRequestHandler<ValidateDbmsDictionaryCommand, Result>
{
    public async Task<Result> Handle(ValidateDbmsDictionaryCommand command, CancellationToken ct)
    {
        var spec = new DbmsProbeSpec(
            command.DbmsSystemName, command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        return await probe.ProbeAsync(spec, ct);
    }
}
