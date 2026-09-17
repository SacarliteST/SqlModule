using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.CreateDbmsDictionary;

internal record CreateDbmsDictionaryCommand(
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
    string DefaultPassword) : IRequest<Result<DbmsDictionaryResponse>>;

internal sealed class CreateDbmsDictionaryHandler(AppDbContext db, IDbmsProbe probe)
    : IRequestHandler<CreateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>
{
    public async Task<Result<DbmsDictionaryResponse>> Handle(
        CreateDbmsDictionaryCommand command, CancellationToken ct)
    {
        if (await db.DbmsDictionaries.AnyAsync(x => x.DbmsName == command.DbmsName, ct))
        {
            return Result<DbmsDictionaryResponse>.Fail(DbmsDictionaryErrors.AlreadyExists(command.DbmsName));
        }

        var spec = new DbmsProbeSpec(
            command.DbmsSystemName, command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        var probeResult = await probe.ProbeAsync(spec, ct);
        if (!probeResult.IsSuccess)
        {
            return Result<DbmsDictionaryResponse>.Fail(probeResult.Error!);
        }

        var entity = Domain.DbmsCatalog.DbmsDictionary.Create(
            command.DbmsName, command.DbmsSystemName, command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        db.DbmsDictionaries.Add(entity);
        await db.SaveChangesAsync(ct);
        return DbmsDictionaryMappings.ToResponse(entity);
    }
}
